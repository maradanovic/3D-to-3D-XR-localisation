#include <inttypes.h>
#include <mutex>
#include <signal.h>
#include <Sockets/ServerSocket.hpp>
#include <stdio.h>
#include <stdlib.h>
#include <thread>
#include <unordered_set>

//for file in and out
#include <fstream>
using std::ofstream;

//Script modified from: https://github.com/rhymu8354/SocketTutorial

namespace
{

    std::string fullMessage;

    std::string transformMatrixString;

    const std::shared_ptr<Sockets::ServerSocket::Client> *myUnityClientAdd;

    //From: https://stackoverflow.com/questions/53712846/fastest-way-to-read-the-last-line-of-a-string
    inline std::string last_line_of(std::string const &s)
    {
        return s.substr(s.rfind('\n') + 1);
    }

    //Flag to indicate if the registration (localization) was unsuccessful.
    bool registrationUnsuccessful = false;

    // This is the TCP port number on which to accept new connections.
    constexpr uint16_t port = 8013;

    // This holds the set of currently connected clients.
    std::unordered_set<std::shared_ptr<Sockets::ServerSocket::Client>> clients;

    // This is used to serialize access to the set of currently connected
    // clients.
    std::mutex mutex;

    // This flag is set by our SIGINT signal handler in order to cause the main
    // program's polling loop to exit and let the program clean up and
    // terminate.
    bool shutDown = false;

    //From: https://stackoverflow.com/questions/874134/find-out-if-string-ends-with-another-string-in-c
    bool hasEnding(std::string const &fullString, std::string const &ending)
    {
        if (fullString.length() >= ending.length())
        {
            return (0 == fullString.compare(fullString.length() - ending.length(), ending.length(), ending));
        }
        else
        {
            return false;
        }
    }

    //Reads the python output (transformation matrix txt) and converts to string to be sent to the client.
    void ReadAndSerializeMatrix()
    {
        transformMatrixString.clear();

        std::ifstream in;
        in.open("resultTransformationMatrix.txt");
        if (in.is_open())
        {
            std::string line;
            while (in)
            {
                std::getline(in, line);
                transformMatrixString += line + "|";
            }
            transformMatrixString = transformMatrixString.substr(0, transformMatrixString.length() - 2);
            in.close();
        }

        //Prints the received transformation matrix
        printf("Read matrix: \n%s\n", transformMatrixString.c_str());

        //Delete the transformation matrix that has been read.
        // if (remove("resultTransformationMatrix.txt") != 0)
        // {
        //     printf("Error deleting the transformation matrix file\n");
        // }
    }

    //Funcion that is called when the whole mesh is received. It:
    //1. Reads the input string and outputs it to an ASCII .ply.
    //2. Starts the python script (the registration) and receives the exit code from it.
    //3. If the exit code is ok, it reads the output (transformation matrix txt) and converts it into string.
    void OnMeshReceived(
        std::string receivedMesh)
    {
        //Strings to hold the .ply header and the total number of vertices and triangles (faces).
        std::string plyHeader;
        std::string verticesNumString;
        std::string trianglesNumString;

        //Reads the number of triangles from the last line of the received string and removes it.
        trianglesNumString = last_line_of(receivedMesh);
        receivedMesh = receivedMesh.substr(0, receivedMesh.length() - (trianglesNumString.length() + 1));

        //Reads the number of vertices from the next last line and removes it.
        verticesNumString = last_line_of(receivedMesh);
        receivedMesh = receivedMesh.substr(0, receivedMesh.length() - verticesNumString.length());

        //Constructs the header containing the number of vertices and triangles (faces).
        plyHeader = "ply\nformat ascii 1.0\nelement vertex " + verticesNumString + "\nproperty float x\nproperty float y\nproperty float z\nelement face " + trianglesNumString + "\nproperty list uchar int vertex_indices\nend_header\n";

        //Outputs the mesh to a .ply file
        std::ofstream outFile;
        outFile.open("data_ar_localization/mesh_ascii_0.ply");
        outFile << plyHeader;
        outFile << receivedMesh;
        outFile.close();

        //Starts the python script and checks the return exit code.
        int returnCode = system("python registration_ar_localization.py");

        std::string returnCodeStr = std::to_string(WEXITSTATUS(returnCode)).c_str();

        if (returnCode < 0)
        {
            printf("Python return error (unknown error).\n");
            registrationUnsuccessful = true;
        }
        else
        {
            if (WIFEXITED(returnCode))
            {
                if (returnCodeStr == "0")
                {
                    printf("Python return code 0\n");
                    registrationUnsuccessful = false;
                    ReadAndSerializeMatrix();
                }
                else if (returnCodeStr == "1")
                {
                    printf("Python return code 1\n");
                    registrationUnsuccessful = true;
                }
                else
                {
                    printf("Unrecogized Python return exit code: %s\n", returnCodeStr.c_str());
                    registrationUnsuccessful = true;
                }
            }
            else
            {
                printf("Python exited abnormally\n");
                registrationUnsuccessful = true;
            }
        }
    }

    // }

    // This function is provided to the event loop to be called if a client
    // connection is closed for reading from the other end.
    //
    // When this happens, remove the client from the set of connected clients.
    // This closes the connection from our end and releases system resources
    // used for the connection.
    void OnClosed(
        const std::shared_ptr<Sockets::ServerSocket::Client> &client)
    {
        std::lock_guard<decltype(mutex)> lock(mutex);
        (void)clients.erase(client);
        printf("Client connection closed (%zu remain).\n", clients.size());
    }

    // This function is provided to the event loop to be called whenever a
    // message is received from a client connection.
    //
    // When this happens, simply print the message to standard output.
    void OnReceiveMessage(
        const std::shared_ptr<Sockets::ServerSocket::Client> &client,
        const std::string &message)
    {
        printf("Message received\n");

        //Because the incoming mesh can be a very long string, it does not fit in one package (message).
        //So, the incoming message is appended to the string that holds the full message.
        fullMessage += message;

        //After each received message, check if the ending contains the notification that the mesh is sent.
        //If yes, start the
        if (hasEnding(fullMessage, "Mesh sent!"))
        {
            printf("Mesh sent message detected!\n");

            //Remove the mesh sent notification from the end and start the function that outputs
            //it and starts the python script (registration).
            fullMessage = fullMessage.substr(0, fullMessage.length() - 10);
            OnMeshReceived(fullMessage);

            //Clear the full message string to be ready for the next incoming mesh.
            fullMessage.clear();

            //Check the string that should hold the output of python (transformation matrix).
            //Check if the RegistrationUnsuccessful flag is up.
            //If both are false, proceed with sending the output (transformation matrix) to the client.
            //This should run after the OnMeshReceived() is finished.
            if (transformMatrixString.empty())
                printf("Error: Transformation matrix not generated.\n");
            else if (registrationUnsuccessful)
                printf("Registration unsuccessful.");
            else
            {
                std::weak_ptr<Sockets::ServerSocket::Client> clientWeak(client);
                const auto myUnityClient = *myUnityClientAdd;
                myUnityClient->SendMessage(transformMatrixString);
                //myUnityClient->SendMessage("\n");
                myUnityClient->SendMessage("Transformation matrix sent!\n");
                printf("Matrix sent!\n");
            }

            printf("Just a test.\n");
        }

        // if (message == "Mesh sent!")
        // {
        //     printf("True!\n");
        //     //fullMessage = fullMessage + message;
        //     printf("Message:\n %s\n", message.c_str());
        //     std::weak_ptr< Sockets::ServerSocket::Client > clientWeak(client);
        //     const auto myUnityClient = *myUnityClientAdd;
        //     myUnityClient->SendMessage("Mesh received!\n");
        //     printf("Message sent - the line is done at least\n");
        //     //OnMeshSent();
        //     //printf("Full message:\n %s\n", fullMessage.c_str());
        //     exit;
        // }
        // else
        // {
        //     fullMessage += message;
        //     //printf("Message:\n %s\n", message.c_str());
        //     exit;
        // }

        // if (hasEnding(fullMessage, "Mesh sent!"))
        // {
        //     printf("this is some win! after\n");
        // }
    }

    void OnAcceptClient(
        std::shared_ptr<Sockets::ServerSocket::Client> &&client)
    {
        // Add the client to the set of connected clients.  If this client
        // was already in the set, return early (this should never happen,
        // but it's cheap to test).
        std::lock_guard<decltype(mutex)> lock(mutex);
        const auto clientInsertion = clients.insert(std::move(client));
        if (!clientInsertion.second)
        {
            return;
        }

        // Start the event loop of the connection object representing the
        // client, setting delegates to handle when messages are received
        // or when the connection is closed for reading from the other end.
        printf("New connection accepted (%zu total).\n", clients.size());
        const auto &clientsEntry = clientInsertion.first;
        const auto &newClient = *clientsEntry;
        std::weak_ptr<Sockets::ServerSocket::Client> clientWeak(newClient);
        myUnityClientAdd = &newClient;
        newClient->Start(
            // onReceived
            [clientWeak](const std::string &message)
            {
                auto client = clientWeak.lock();
                if (!client)
                {
                    return;
                }
                OnReceiveMessage(client, message);
            },

            // onClosed
            [clientWeak]
            {
                auto client = clientWeak.lock();
                if (!client)
                {
                    return;
                }
                OnClosed(client);
            });

        // Send a message to the client to test the server's ability to send a
        // message as well as the client's ability to receive it.
        //newClient->SendMessage("Welcome!\n");
        //printf("Welcome message sent!\n");
    }

    // This function is set up to be called whenever the SIGINT signal
    // (interrupt signal, typically sent when the user presses <Ctrl>+<C> on
    // the terminal) is sent to the program.  We just set a flag which is
    // checked in the program's polling loop to control when the loop is
    // exited.
    void OnSigInt(int)
    {
        shutDown = true;
    }

    // This is the function called from the main program in order to operate
    // the socket while a SIGINT handler is set up to control when the program
    // should terminate.
    int InterruptableMain()
    {
        // Make a socket and assign an address to it.
        Sockets::ServerSocket server;
        if (!server.Bind(port))
        {
            return EXIT_FAILURE;
        }

        // Set up the socket to receive incoming connections.
        server.Listen(OnAcceptClient);
        printf("Now listening for connections on port %" PRIu16 "...\n", port);

        // Poll the flag set by our SIGINT handler, until it is set.
        while (!shutDown)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(100));
        }
        printf("Program exiting.\n");
        return EXIT_SUCCESS;
    }

}

int main(int argc, char *argv[])
{
    // Catch SIGINT (interrupt signal, typically sent when the user presses
    // <Ctrl>+<C> on the terminal) during program execution.
    const auto previousInterruptHandler = signal(SIGINT, OnSigInt);
    const auto returnValue = InterruptableMain();
    (void)signal(SIGINT, previousInterruptHandler);
    return returnValue;
}
