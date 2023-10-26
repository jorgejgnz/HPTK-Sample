# v57 Release Notes and Upgrade Guide

## Upgrading
### From Any Version
You will always get the cleanest experience by deleting your Assets/Oculus/Voice directory between updates, but it shouldn't be required.

### From The Last Version
If you’re coming from v56, upgrading should be relatively smooth. You should be able to simply install the latest package on top of your existing install. As always deleting the Oculus/Voice directory before upgrading will provide the smoothest experience.

### From Older Versions
It is required that you delete your existing 'Assets/Oculus/Voice/Demo' directory before making this upgrade in order to remove the previous samples that may conflict with the newly updated samples.  As always deleting the Oculus/Voice directory before upgrading will provide the smoothest experience.  Simply installing the package should work and the Oculus cleanup tools will remove old files.
As of v49 we made a change to update our namespaces to match the new Meta branding. This means any code references you may have had to Voice SDK will need to have their using statements updated.


## What’s New
### In v57
* Coming Soon

### In v56
* TTS speakers now have pause & resume functionality.
* TTS speaker events added for OnComplete which is called following load failure, load abort, playback cancellation or playback completion.
* TTS voice sample updated to include pause button, to allow info scrolling & to use tts speaker request events via async sfx toggle.

### In v55
* Samples are now accessible in the Voice Hub.
* All samples have been cleaned up and split into their own directory to make them easier to follow.
* TTS voices sample improved to make trying out different voices & ssml sfx effects easier.
* TTS recently added a set of new voices. TTSWit component now has a dropdown to add new presets with voices you may not have used yet.
