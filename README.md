# CS 445 AI Project: UAS Path Planning System (Unity)

## Team 7: Lemuel Espinosa, Olivia Gonzalez, and Tyler Kingston

## Overview

This project implements a 3D path planning system for an unmanned aerial system (UAS) operating in an obstacle dense environment, such as a city. This system generates a grid of voxels containing wieghts, assigns traversal costs, then computes the path between the user defined start and end points using the A\* algorithm.

This project was made in Unity and programmed in C#.

---

## How to Run the Project

1. Navigate to the Build/ folder
2. Unzip FinalUASPathPlanningBuild.zip
3. Open UASPathPlanning.exe

---

## How to Use the Program

### Controls:

- Move around: W/A/S/D
- Move up: E
- Move down: Q
- Move faster: Left shift
- Lock/Unlock cursor: Right Click

The program starts off with the cursor unlocked. To look around with the mouse, press right click which locks the cursor in the middle of the screen.

### Generate Start and End Points

To generate the start and end points, first unlock the cursor, then click on "Create Start/End Points" at the top left of the screen. It will prompt you to place the start point. Click anywhere on the map using left click. Then a second point for the end point.

### Generate Path

One both the start and end points have been generated, you can generate the path by clicking on "Begin Search" at the top right of the screen. This will create a path from the start and end point, avoiding all the building structures.

### Generate new path

A new path can be generate by clicking on "Create Start/End Points" again and generating the new points.
