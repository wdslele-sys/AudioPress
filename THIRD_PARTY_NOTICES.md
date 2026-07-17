# Third Party Notices

## FFmpeg / ffprobe

AudioPress uses FFmpeg and ffprobe as external command-line tools for media probing, compression, and conversion.

The packaging script downloads the Windows LGPL build from BtbN FFmpeg Builds by default:

https://github.com/BtbN/FFmpeg-Builds/releases

Default download URL:

https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-lgpl.zip

The packaged ZIP includes `tools/ffmpeg/FFMPEG_VERSION.txt` generated from `ffmpeg.exe -version`. Review that file for the exact build configuration included in a release.

FFmpeg is licensed under LGPL or GPL depending on build configuration and enabled libraries. AudioPress must not redistribute builds configured with `--enable-nonfree`. If you change `AUDIOPRESS_FFMPEG_URL`, verify the downloaded build's license and update this notice before publishing.

For slow networks, maintainers may pre-place `ffmpeg.exe` and `ffprobe.exe` in `tools/ffmpeg` or point `AUDIOPRESS_FFMPEG_URL` to a trusted mirror. Do not publish binaries from an unverifiable mirror.

FFmpeg project:

https://ffmpeg.org/
