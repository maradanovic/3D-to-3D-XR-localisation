# 3D-to-3D-XR-localisation
This repo contains the server and the cliend side of the Augmented Reality/Mixed Reality localization system based on automated 3D to 3D model registration.

The server side is comprised of two main components:
1. Deep neural network that performs the automated 3D to 3D model registration, and
2. C++ server for communication with the client (AR/MR device).

The first part is a modified TensorFlow implementation of [D3Feat](https://github.com/XuyangBai/D3Feat), published at CPRV'2020 in a paper titled "D3Feat: Joint Learning of Dense Detection and Description of 3D Local Features" by Xuyang Bai, Zixin Luo, Lei Zhou, Hongbo Fu, Long Quan and Chiew-Lan Tai and forked from D3Feat.

The client side is a Unity project containing a set of C# scripts that perform the mesh extraction, communication with the server, and other tasks.

## Introduction

To be added after the paper is published.

## Installation (server side)

Tested and working with Ubuntu 16.04, TensorFlow 1.12.0, CUDA 9.0 and cuDNN 7.4.

Make sure all required packages are installed by running:

        sudo apt install gcc python3-dev python3-pip python3-tk libxml2-dev libxslt1-dev zlib1g-dev g++ cmake build-essential libssl-dev libffi-dev

Install [CUDA](https://docs.nvidia.com/cuda/cuda-installation-guide-linux/index.html) and [cuDNN](https://docs.nvidia.com/deeplearning/cudnn/install-guide/index.html).

Install [TensorFlow](https://www.tensorflow.org/install/pip).

Create the environment and install the required libaries to run D3Feat:

        conda env create -f environment.yml

Compile the customized Tensorflow operators located in tf_custom_ops. Open a terminal in this folder, and run:

        sh compile_op.sh

Compile the C++ extension module for python located in cpp_wrappers. Open a terminal in this folder, and run:

        sh compile_wrappers.sh

Check installation by running the demo registration:

        python demo_registration.py

Compile the server. Open the server folder in VSCode and compile it by pressing F7.

Instructions

The point cloud that serves as a reference should be put into the ar_localization folder and named "reference_cloud_bin.ply". Before running the server, the descriptors for the reference must be prepared. Open the terminal in root and run:

        python prepare_reference.py

You must ensure the reference point cloud on the server and the reference point cloud used by the client are exactly the same and in the same coordinate frame. For example, if using Unity, a left-handed Y-up coordinate frame should be used.

By default, the server uses port 8013. This can be changed in the server source.

To start the server, open the terminal in root and run:

        server/build/Server/Server

## Installation (client side)

Load the Unity project located in the unity folder. Unity version is 2019.4 LTS.

Copy the point cloud you want to use as a reference in the Assets/Clouds folder (or anywhere else in the Assets) in the .ply format. Add it to the scene and ensure it's position and rotation are (0, 0, 0).

The scene contains a GameObject named World. All GameObjects that you want to align with the real would must be placed as children to World. 

The reference point cloud is the representation of the real world. Use it as a scaffold to place virtual objects (GameObjects that are children to World) in desired places. For example, you can place virtual furniture and other objects within the flat as shown in [this](https://www.youtube.com/watch?v=Vght8jJMfv0&) video.

Make sure you turn off or delete the reference point cloud from the scene before deploying on an AR/MR device, as it heavily impacts the performance and is not needed for localization. If you leave it on, keep in mind the shader is not modified for AR so it will only render on one eye, while the point shader will not render at all as most AR/MR devices cannot render points.

### Notes on Unity's coordinate system

You must ensure the point cloud is in Unity's coordinate system, which is a left-handed Y-up coordinate system. If you have data in a normal, right-handed Z-up coordinate system, you should convert it to Unity's. To do this, use any point cloud editing software, such as CloudCompare, and:

1. Rotate the point cloud around the X axis for 90 degrees,
2. Multiply the Y coordinate with -1 (rescale the point cloud with (1, -1, 1)).

The same point cloud, in Unity's coordinates system, must be used as a scaffold in Unity (client side) and as a reference on the server side.

### Additional notes:

* The project uses Keijiro Takahashi's [Pcx point cloud importer/renderer for Unity](https://github.com/keijiro/Pcx).

* The project uses Microsoft's [Mixed Reality Toolkit (MRTK)](https://github.com/microsoft/MixedRealityToolkit-Unity).

* MRTK version is 2.7. If you want to upgrade to a newer version, you must use the [Mixed Reality Feature Tool](https://docs.microsoft.com/en-us/windows/mixed-reality/develop/unity/welcome-to-mr-feature-tool).
