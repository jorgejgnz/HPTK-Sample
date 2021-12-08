How To build Stereo180Video Sample for Android

Step 1: Acquire your 180 video file (you can find a few examples here: https://creator.oculus.com/blog/peak-quality-3d-180-immersive-video-in-oculus-go-and-gear-vr/?locale=en_US)
Step 2: Either place you video in StreamingAssets (if the file size is < 1gb), or push the video to you device into the obb directory for your application:
	`adb push <video file> /sdcard/Android/obb/<your package name>/`
Step 3: Open the Stereo180Video scene. You will need to populate "Movie Name" field of the "MoviePlayer" object's SampleMoviePlayer component with your video path. If the file is in StreamingAssets, you can just put in the video name. If you pushed your video to your device, enter: `file:///sdcard/Android/obb/<your package name>/<your video file>`
Step 4: From the menu, select `Oculus > Samples > Video > Enable Native Android Video Player`

When deploying to the Oculus store, you can upload your video as a 'required asset', which will put it in the obb file when the app is installed. See here for more details on using required assets: https://developer.oculus.com/documentation/native/ps-dlc-rift/#generic-asset-files
