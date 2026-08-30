================================================================================
samples/assets/video — the sample video clips
================================================================================

Seven short clips, consumed by samples/CodeBrixPlatform/VideoPlayerDemo. Two
recordings, in three container flavours each, about 0.5-0.8 MB apiece - plus a
seventh, the landscape Mode 2 encode again with three chapters in it.

PROVENANCE AND LICENCE
======================
Both recordings were made by Jeremy Ellis on his phone and PLACED BY HIM IN THE
PUBLIC DOMAIN on 2026-08-29. Everything here is derived from those two files and
is therefore Public Domain too. No third-party footage, no stock library, no
attribution requirement.

They were encoded by the CodeBrix.VideoPlayback authoring corpus generator; the
authoritative record of exactly how - every encoder setting and every command
line - is that repository's tests/assets/authoring/MANIFEST.txt. Six of these
files are copies of the 720p entries of that corpus, renamed so the flavour is
visible in the file name. The seventh (landscape_720p_mode2_chapters.cbv) was
authored on 2026-08-30 with the published CodeBrix.VideoPlayback.Authoring
package, from the same Public-Domain landscape recording with the same 720p
settings (libsvtav1 preset 4, crf 24, 30 fps, keyframe every 60 frames,
libvorbis 96 kbit/s 48 kHz stereo) and the three-chapter ffmetadata file that
sits beside it, landscape_720p_mode2_chapters.ffmeta.

THE FILES
=========
  landscape_720p_webm.webm   AV1 video + Opus audio, WebM container. The
  portrait_720p_webm.webm     "path 1" case: an ordinary WebM file that any
                              browser also opens.

  landscape_720p_mode1.cbv   AV1 video + Opus audio, CodeBrix Mode 1 - the
  portrait_720p_mode1.cbv     WebM-profile flavour of the .cbv format. Byte for
                              byte a valid WebM file; only the extension differs.

  landscape_720p_mode2.cbv   AV1 video + Vorbis audio, CodeBrix Mode 2 - the
  portrait_720p_mode2.cbv     bespoke flavour, written by the playback library's
                              own muxer. Needs no Opus package to play.

  landscape_720p_mode2_chapters.cbv
                              The landscape Mode 2 clip with THREE CHAPTERS
                              (Opening / Middle / Closing at 0.0, 1.4 and 2.8 s),
                              each titled in English and German - the bespoke
                              flavour keeps per-language chapter titles. This is
                              the clip that lights up the demo's chapter list.
  landscape_720p_mode2_chapters.ffmeta
                              The ffmetadata [CHAPTER] file it was authored from.

The portrait pair is a phone recording held upright: it is here so that the
letterboxing (Stretch = Uniform) has something to letterbox, and a portrait
picture in a landscape window is what that looks like.

WHAT IS NEEDED TO PLAY THEM
===========================
AV1 decoding for all seven: the application references
CodeBrix.VideoPlayback.Dav1d.BsdLicenseForever and calls
CodeBrixVideoPlaybackDav1d.Register(). The Opus soundtrack of the WebM and
Mode 1 files additionally needs CodeBrix.Audio.Opus.BsdLicenseForever and
CodeBrixAudioOpus.Register(). The Mode 2 files carry Vorbis, which needs
neither.

REGENERATING THEM
=================
They are committed golden assets and are not built by anything in this
repository. To produce a fresh corpus, run the authoring tool in the
CodeBrix.VideoPlayback repository and copy the 720p files across again; the
chaptered clip is re-authored with CbvAuthor.Write (Flavour = Bespoke, the
settings above, ChaptersPath = the .ffmeta file beside it). Do not
pin these by hash: the Matroska muxer writes a random track UID and a fresh
muxing date into every file, so two runs never produce identical bytes.
