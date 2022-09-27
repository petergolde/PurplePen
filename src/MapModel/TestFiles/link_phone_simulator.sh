#!/bin/bash
# Execute this script from the directory it lives in.
# Creates a symbolic link in the iPhone simulator directory to the test files directory.
# Parameter1: GUID of the iPhone simulator application.
ln -s $PWD "$HOME/Library/Application Support/iPhone Simulator/7.1/Applications/$1/Documents/TestFiles" 
