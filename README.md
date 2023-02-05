| Release Weekly | CI/CD-Master | CI/CD Fast-Recording |
| -------------------- | -------------------- | -------------------- |
| ![Build status](https://dev.azure.com/MerlinCooper/iRacingReplayDirector/_apis/build/status/iRacingReplayDirector_Release_Weekly) | ![Build Status](https://dev.azure.com/MerlinCooper/iRacingReplayDirector/_apis/build/status/iRacingReplayDirector_Master?branchName=master) | ![Build Status](https://dev.azure.com/MerlinCooper/iRacingReplayDirector/_apis/build/status/iRacingReplayDirector_OBS%20Fast%20Record%20Branch%20(alpha)?branchName=Fast_Video_Recording_With_OBS) |

iRacingReplayDirector
=====================

This program converts your iRacing replays into video files complete with leaderboards, fastest laps, and more. You can upload these to YouTube and/or use them however you choose.

The program then does the following:
* Analyses your replay session - looking for all the spins and crashes etc. - so it makes sure it can capture those.
* Captures some scenic views of the track - to use as a backdrop for the introduction sequence which will include qualifying positions.
* Captures the entire race.
* Encodes the results which will add the leaderboard information and create the video file(s). 

Here is an example of a full length video created by Replay Director:
https://youtu.be/L8Kd0OqsEVg

This is a highlight video:
https://youtu.be/a1WaRx0R9KI 

Requirements
============
* Video capture software (ie. OBS, Nvidia, etc.).
* The program will use Alt-F9 for recording. Make sure your video capture software is also set to use that keystroke.
* Set your video capture software to only record the iRacing replay window.
* Your video capture software must save the video as an .mp4 or .avi file.
* Replays should capture all competitors. Before the session, under the graphics options in iRacing, set the Max Cars box to a number greater than number of competitors.
* Only tested with the PCM audio codec
* Only supports MPEG/H.264 video codec for capture/conversion

Known Issues
============

- Microsoft security features do prevent loading the plugin dll's used for transcoding. [If transcoding doesn start check whether the dlls are blocked.](https://github.com/MerlinCooper/iRacingReplayDirector/issues/91#issuecomment-1417442193). Looking for a permanent corrective action - any advise highly appreciated.  

HOW TO USE
==========

1. The first time you use the software, you’ll want to verify it is talking to your Video Capture software correctly. 
2. Open your Video Capture software
3. Open an iRacing replay.
4. Click the Verify Video Capture under the Race Capture tab
5. Browse to the directory your Video Capture software uses when it captures a video file. You may have to cut/paste the directory name here.
6. Once the directory is selected, press Verify Video Capture.
7. The program will run a quick test to verify it can record a video and it is placed in the expected directory.
8. Once this step is complete and working, you need to Verify the Video Encoding process.
9. Go to the Video Encoding tab
10. Press Verify Video.
11. Browse and select the video that was recorded in step 6.
12. Click Verify Conversion.
13. If all this worked correctly, you’ll find 1 or 2 .wmv files in the directory. These are your encoded videos. You’ll only have 1 if you checked the Highlight Video Only checkbox before encoding.
14. If you’d like, you can now do a test run using the “Short Test Only” checkbox to verify everything is working. This is preferable to doing a 40 minute video and finding out something wasn’t setup correctly. Just follow the instructions below with the “Short Test Only” box checked.

Creating a Video Thereafter
===========================
1. Start iRacing with the replay you wish to convert. Make sure iRacing is running at an optimal resolution for video encoding. eg: 720p or 1080p.
2. Press the space bar to remove iRacing’s overlay from the replay screen.
3. Start your Video Capture software and verify it is ready to go.
4. Start iRacing Replay Director
5. If this is the first time you’ve used the track in the replay, click "Configure Track Cameras".
6. If iRacing is running, it will have the track selected.
7. Select the % options for the cameras you would like to use during the replay. Once you have done this for a track, you won’t have to repeat this in the future.
8. If you want the video encoding to happen automatically when the capture is complete, check the “Encode Video After Capture” box.
9. Press the Begin Capture button when ready to capture.
10. The process will run for at least the entire length of the original replay so be patient.
11. Once the race capture is completed, you can then Encode your full race and highlight videos.
12. When encoding, if the highlight Video Only isn’t checked, it creates 2 videos from your replays; the full replay and a highlight video with a length defined in the Config options. If the Highlight Video Only box is checked under Video Encoding, it will only create the highlight video.
13. The completed videos have a .wmv extension

History
==============
iRacingReplayDirector is the sucessor of the discontinued iRacingReplayOverlay.NET application developed by Vipoo. 

As Vipoo no longer owns an active license for iRacing - nor has the time to commit to this project - I will take over managing the original repository (vipoo/iRacingReplayDirector.net) and continue to coordinate future enhancements as well.  

For the ease of processing most of the development activites will be managed in the fork MerlinCooper/iRacingReplayDirector. 

Based on Azure DevOps continous integration is setup for two branches "master" and "Fast_Video_Recording_With_OBS". Whereas the master branch is being used for changes/modifications within the current program structure and the "Fast_Recording_With_OBS" branch is taking a different approach to significantly reduce the time to create a highlight video from long replays. 
