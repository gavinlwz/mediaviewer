#pragma once
#pragma warning(disable : 4244)
// unsafe function warning disable
#pragma warning(disable : 4996)
#include <algorithm>
#include "stdafx.h"
#include "VideoInit.h"
#include "Stream.h"
#include <msclr\marshal.h>

namespace VideoLib {

typedef void (__stdcall *LOG_CALLBACK)(int level, const char *message);

class Video {
	
protected:

	std::vector<VideoLib::Stream *> stream;
	AVFormatContext *formatContext;
	
	int videoIdx;
	int audioIdx;
	
	static LOG_CALLBACK logCallback;

	Video() {

		formatContext = NULL;

		videoIdx = -1;
		audioIdx = -1;

		VideoInit::initializeAVLib();
			
	}

	static void libAVLogCallback(void *ptr, int level, const char *fmt, va_list vargs)
    {
		char message[65536];   
		const char *module = NULL;

		// if no logging is done or level is above treshold return
		if(logCallback == NULL || level > av_log_get_level()) return;
	
		// Get module name
		if(ptr)
		{
			AVClass *avc = *(AVClass**) ptr;
			module = avc->item_name(ptr);
		}

		std::string fullMessage = "FFMPEG";

		if(module)
		{
			fullMessage.append(" (");
			fullMessage.append(module);
			fullMessage.append(")");
		}

		vsnprintf(message, sizeof(message), fmt, vargs);

		fullMessage.append(": ");
		fullMessage.append(message);

		// remove trailing newline
		fullMessage.erase(std::remove(fullMessage.begin(), fullMessage.end(), '\n'), fullMessage.end());

		logCallback(level, fullMessage.c_str());
	
    }

	virtual void addStream(VideoLib::Stream *stream) {

		this->stream.push_back(stream);

		if(stream->getCodecContext()->codec_type == AVMEDIA_TYPE_VIDEO) {

			videoIdx = int(this->stream.size() - 1);

		} else if(stream->getCodecContext()->codec_type == AVMEDIA_TYPE_AUDIO) {

			audioIdx = int(this->stream.size() - 1);
		}
	}
	
public:


	enum FrameType {
		VIDEO,
		AUDIO
	};

	virtual ~Video() {

		disableLibAVLogging();
		close();
	}

	virtual void close() {
		
		if(formatContext != NULL) {
				
			for(unsigned int i = 0; i < stream.size(); i++) {
				
				delete stream[i];
			}
						
			stream.clear();		
		} 
	
		videoIdx = -1;
		audioIdx = -1;
	
	}

	virtual AVFormatContext *getFormatContext() const {

		return(formatContext);
	}

	virtual VideoLib::Stream *getStream(int i) const 
	{
		return(stream[i]);
	}

	virtual int getVideoStreamIndex() const {

		return(videoIdx);
	}

	virtual int getAudioStreamIndex() const {

		return(audioIdx);
	}

	virtual void setVideoStreamIndex(int idx) {

		videoIdx = idx;
	}

	virtual void setAudioStreamIndex(int idx) {

		audioIdx = idx;
	}

	virtual unsigned int getNrStreams() const {
		 
		return((unsigned int)stream.size());
	}

	virtual AVStream *getVideoStream() const {

		return(stream[videoIdx]->getAVStream());
	}

	virtual const AVCodec *getVideoCodec() const {

		return(stream[videoIdx]->getCodec());
	}

	virtual AVCodecContext *getVideoCodecContext() const {

		return(stream[videoIdx]->getCodecContext());
	}

	virtual AVStream *getAudioStream() const {

		return(stream[audioIdx]->getAVStream());
	}

	virtual const AVCodec *getAudioCodec() const {

		return(stream[audioIdx]->getCodec());
	}

	virtual AVCodecContext *getAudioCodecContext() const {

		return(stream[audioIdx]->getCodecContext());
	}

	
	virtual bool hasVideo() const {

		return(videoIdx >= 0);
	}

	virtual bool hasAudio() const {

		return(audioIdx >= 0);
	}
	

	static void enableLibAVLogging(int logLevel = AV_LOG_ERROR) {
	
		av_log_set_level(logLevel); 
		av_log_set_callback(&Video::libAVLogCallback);			
			
	}

	static void disableLibAVLogging() {

		av_log_set_callback(NULL);	
	}

	static void setLogCallback(LOG_CALLBACK logCallback) 
	{
		Video::logCallback = logCallback;
	}

	static void writeToLog(int level, char *message) {

		if(logCallback != NULL) {

			logCallback(level, message);
		}
	}
};



}