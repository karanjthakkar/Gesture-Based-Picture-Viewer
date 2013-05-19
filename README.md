# Gesture Based Picture Viewer

---

**Table of Contents**  *generated with [DocToc](http://doctoc.herokuapp.com/)*

- [Introduction](#introduction)
- [Functionalities Implemented](#functionalities-implemented)
- [Prerequisities](#prerequisities)
- [Features to be modified/added](#features-to-be-modifiedadded)
- [Note](#note)
- [Preview](#preview)
- [Downloading the source code](#downloading-the-source-code)
- [Contributors](#contributors)
- [LICENSE](#license)

---

### Introduction

This is the code of the WPF application for a Gesture Based Picture Viewer using Microsoft Kinect. 

The code has been tested on the following configuration:

1. Windows 7 Professional (64-bit)
2. Visual Studio 2012 Ultimate

---

### Functionalities Implemented

1. Swipe Left
2. Swipe Right
3. Zoom In
4. Zoom Out
5. Panning
6. Moving mouse cursor
7. Switching to different folders
8. Perform Left Mouse Click using Speech Recognition by saying the word "Click"

See the Preview below to get a feel of how to use those gestures.

---

### Prerequisities

Before you run the Visual Studio Solution file (.sln) as it is, please make sure that:

1. You have Kinect SDK installed (My version 1.5)
2. You have Speech Platform SDK installed (My version 11.0)
2. You have extracted the rar archive named `Extract.rar` on your `Desktop`. It is important that you extract it on your Desktop **ONLY**!

---

### Features to be modified/added

1. The range of the pan is very less. What I mean is, once the image is zoomed to some extent, panning using the right hand **DOES NOT** cover the entire picture. I've tried some modifications of my own, but none that work neatly.

2. Efficient sppech recognition. The code right now has speech recognition included but it only understands the word "Click", as in, you can use that to trigger a mouse click. I need to put up some more words into its recognizer.

---

### Note

A lot of the lines in the code maybe redundant or unnecessary. It is my first attempt at making a Windows app using C# and XAML. This code is by no means perfect or elite. So, fork this repo, see if there are any modifications you can make that improve the code and make a pull request and I'll merge any changes that I feel make this code better than it previously was. I'll also add your name to the contributors list. Lets make this code as clean and neat as possible.

---

### Preview

See it in action [here](http://youtu.be/v8SumS-I1qo)

---

### Downloading the source code

For users unfamiliar with `github`: You can download the source code as a zip file by clicking [here](https://github.com/karanjthakkar/Gesture-Based-Picture-Viewer/archive/master.zip)

For users who have used github before: You can simply fork the repository(if you intend to make changes and pull them to master) and/or clone it on your local machine

---

### Contributors

1. [Navya Manoj](http://in.linkedin.com/pub/navya-manoj/29/739/592)

---

### LICENSE

[Simplified BSD](http://en.wikipedia.org/wiki/BSD_licenses#2-clause_license_.28.22Simplified_BSD_License.22_or_.22FreeBSD_License.22.29)(See the [LICENSE](https://github.com/karanjthakkar/Gesture-Based-Picture-Viewer/blob/master/LICENSE.txt) file)

For any feedback, comment, advice, bugs, please contact me at:
**karanjthakkar [at] gmail [dot] com**
