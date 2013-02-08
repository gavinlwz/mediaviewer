#pragma once
#include "stdafx.h"
#include <sstream>
#include <iostream>
#include <iomanip>
#include <float.h>
#include <vector>
#include "VideoDecoder.h"
#include "WindowsFileUtil.h"


class VideoFrameGrabber : public VideoDecoder {

private:

	std::string videoLocation;
	std::string videoFileName;

	int thumbWidth;
	int thumbHeight;

	int frameNr;

	std::string formatTime(double timeStamp, int &hours, int &minutes, int &seconds) const {

		long totalSeconds = (int)timeStamp;

		seconds = int(totalSeconds % 60);
		minutes = int((totalSeconds / 60) % 60);
		hours = int(totalSeconds / 3600);

		std::stringstream ss;
		ss << std::setfill('0');
		ss << std::setw(2) << hours;
		ss << ":";
		ss << std::setw(2) << minutes;
		ss << ":";
		ss << std::setw(2) << seconds;

		return(ss.str());
	}

	int greatestCommonDivisor(int a, int b) const {
	  return (b == 0) ? a : greatestCommonDivisor(b, a % b);
	}

	int round(float r) {
		return (r > 0.0) ? (int)floor(r + 0.5) : (int)ceil(r - 0.5);
	}

	void probe_dict(AVDictionary *dict, const char *name)
	{
		AVDictionaryEntry *entry = NULL;
		if (!dict)
			return;
		//probe_object_header(name);
		while ((entry = av_dict_get(dict, "", entry, AV_DICT_IGNORE_SUFFIX))) {
		
			metaData.push_back(std::string(entry->key) + ": " + std::string(entry->value));
		
		}
		//probe_object_footer(name);
	}

public:

	int durationSeconds;

	int64_t sizeBytes;

	int width;
	int height;

	std::string container;
	std::string videoCodecName;
	std::string pixelFormat;
	std::vector<std::string> metaData;

	double frameRate;

	VideoFrameGrabber() 	
	{
		setDecodedFrameCallback(decodedFrame, this);

		durationSeconds = 0;
		sizeBytes = 0;
		width = 0;
		height = 0;
		container = "";
		videoCodecName = "";

	}

	virtual void open(const std::string &location, AVDiscard discardMode = AVDISCARD_DEFAULT) {

		VideoDecoder::open(location, discardMode);

		// get metadata
		durationSeconds = getDurationSeconds();

		sizeBytes = formatContext->pb ? avio_size(formatContext->pb) : 0;

		width = getWidth();
		height = getHeight();
		
		container = std::string(formatContext->iformat->long_name);
		videoCodecName = std::string(videoCodec->name);
	
		char buf[64];
		av_get_pix_fmt_string(buf, 64, videoCodecContext->pix_fmt);
		pixelFormat = std::string(buf);

		int pos = pixelFormat.find_first_of(' ');
		if(pos != std::string::npos) {

			pixelFormat = pixelFormat.substr(0, pos);
		}

		if(videoStream->avg_frame_rate.den != 0 && videoStream->avg_frame_rate.num != 0) {

			frameRate = av_q2d(videoStream->avg_frame_rate);
			
		} else {

			frameRate = 1.0 / av_q2d(videoStream->time_base);
		}

		probe_dict(formatContext->metadata, "");
	}

	virtual void close() {

		VideoDecoder::close();

		durationSeconds = 0;
		sizeBytes = 0;
		width = 0;
		height = 0;
		container = "";
		videoCodecName = "";

		thumbWidth = 0;
		thumbHeight = 0;

		metaData.clear();

	}

	virtual ~VideoFrameGrabber() {

	}

	void grab(int maxThumbWidth, int maxThumbHeight, 
			int captureInterval, int nrThumbs)
	{
	
		if(getWidth() == 0 || getHeight() == 0) {

			throw std::runtime_error("invalid video stream");
		}

		float widthScale = 1;
		float heightScale = 1;

		if(getWidth() > maxThumbWidth) {
			
			widthScale = maxThumbWidth / (float)getWidth();				
		}

		if(getHeight() > maxThumbHeight) {
			
			heightScale = maxThumbHeight / (float)getHeight();
		}

		thumbWidth = round(getWidth() * std::min<float>(widthScale, heightScale));
		thumbHeight = round(getHeight() * std::min<float>(widthScale, heightScale));

		initImageConverter(PIX_FMT_BGR24, thumbWidth, thumbHeight, SPLINE);

		double duration = getDurationSeconds();

		int nrFrames = 0;

		if(captureInterval == -1) {

			nrFrames = nrThumbs;

		} else {

			nrFrames = duration / captureInterval;

			if(nrFrames == 0) {
				// make sure to grab atleast one frame
				nrFrames = 1;
			}
		}

		double offset = duration * 0.025;
		double step = (duration - offset) / nrFrames;

		for(frameNr = 0; frameNr < nrFrames; frameNr++) {

			double pos = offset + frameNr * step;

			seek(pos);

			if(decode(DECODE_KEY_FRAMES_ONLY, SKIP_AUDIO, 1) != 1) {

				// retry a non-keyframe
				seek(pos);

				if(decode(DECODE_VIDEO, SKIP_AUDIO, 1) != 1) {

					throw std::runtime_error("could not decode any frames");
				}
			}
		}
	}

	int getThumbWidth() const {

		return(thumbWidth);
	}

	int getThumbHeight() const {

		return(thumbHeight);
	}

};

