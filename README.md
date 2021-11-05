# Learning-based AR/MR localization

This repo is the server side of the Augmented Reality/Mixed Reality localization system based on automated 3D to 3D model registration. The client side is (to be added).

The server side is comprised of two main components:
1. Deep neural network that performs the automated 3D to 3D model registration, and
2. C++ server for communication with the client (AR/MR device).

The first part is a modified TensorFlow implementation of [D3Feat](https://github.com/XuyangBai/D3Feat), published at CPRV'2020 in a paper titled "D3Feat: Joint Learning of Dense Detection and Description of 3D Local Features" by Xuyang Bai, Zixin Luo, Lei Zhou, Hongbo Fu, Long Quan and Chiew-Lan Tai and forked from D3Feat.

## Introduction

To be added after the paper is published.

## Installation

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

Compile the server. Open the server folder with VSCode and compile it by pressing F7.

Instructions

The point cloud that serves as a reference should be put into the ar_localization folder and named "reference.ply". Before running the server, the descriptors for the reference must be prepared. Open the terminal in root and run:

        python prepare_reference.py

You must ensure the reference point cloud on the server and the reference point cloud used by the client are exactly the same and in the same coordinate frame. For example, if using Unity, a left-handed Y-up coordinate frame should be used.

By default, the server uses port 8013. This can be changed in the server source.

To start the server, open the terminal in root and run:

        server/build/Server/Server