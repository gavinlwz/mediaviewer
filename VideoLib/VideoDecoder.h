#pragma once
#include "stdafx.h"
#include "Video.h"
#include "VideoLibException.h"
#include "FilterGraph.h"

namespace VideoLib {

typedef void (__stdcall *DECODED_FRAME_CALLBACK)(void *data, AVPacket *packet, AVFrame *frame, Video::FrameType type);

class VideoDecoder : public Video {

protected:

	AVFrame *frame;
	
	SwsContext *imageConvertContext;
	SwrContext *audioConvertContext;

	AVPixelFormat imageConvertFormat;
	AVSampleFormat audioConvertFormat;

	AVFrame *convertedFrame;
	uint8_t *convertedFrameBuffer;

	double startTime;

	void *data;

	DECODED_FRAME_CALLBACK decodedFrame;

	bool closed;

	AVPacket packet;

	// video
	int64_t sizeBytes;
	
	// audio
	int samplesPerSecond;
	int bytesPerSample;
	int nrChannels;

	int durationSeconds;

	FilterGraph *videoFilterGraph;
	FilterGraph *audioFilterGraph;

	static void logCallback(int level, const char *message) {
		
		Console::WriteLine(gcnew System::String(message)); 
	}

public:

	enum SamplingMode {
		FAST_BILINEAR = SWS_FAST_BILINEAR,
		BILINEAR = SWS_BILINEAR,
		BICUBIC = SWS_BICUBIC,
		X = SWS_X,
		POINT = SWS_POINT,
		AREA = SWS_AREA,
		BICUBLIN = SWS_BICUBLIN,
		GAUSS = SWS_GAUSS,
		SINC = SWS_SINC,
		LANCZOS = SWS_LANCZOS,
		SPLINE = SWS_SPLINE
	};

	enum VideoDecodeMode {		
		DECODE_KEY_FRAMES_ONLY,
		DECODE_VIDEO,
		SKIP_VIDEO
	};

	enum AudioDecodeMode {
		DECODE_AUDIO,
		SKIP_AUDIO
	};

	VideoDecoder()
	{
		decodedFrame = NULL;
		data = NULL;

		frame = NULL;

		imageConvertContext = NULL;
		audioConvertContext = NULL;
		convertedFrame = NULL;
		convertedFrameBuffer = NULL;

		startTime = 0;
			
		closed = true;

		samplesPerSecond = 0;
		bytesPerSample = 0;
		nrChannels = 0;
		sizeBytes = 0;

		durationSeconds = 0;

		imageConvertFormat = AV_PIX_FMT_YUV420P;
		audioConvertFormat = AV_SAMPLE_FMT_S16;

		videoFilterGraph = NULL;
		audioFilterGraph = NULL;
	
	}

	virtual ~VideoDecoder() {

		close();
	}

	bool isClosed() {

		return(closed);
	}

	void setDecodedFrameCallback(DECODED_FRAME_CALLBACK decodedFrame = NULL,
		void *data = NULL) 
	{
		this->decodedFrame = decodedFrame;
		this->data = data;
	}

	virtual void close() {

		Video::close();

		if(frame != NULL) {

			av_free(frame);
			frame = NULL;
		}

		if(convertedFrame != NULL) {

			av_free(convertedFrame);
			convertedFrame = NULL;
		}

		if(convertedFrameBuffer != NULL) {

			av_free(convertedFrameBuffer);
			convertedFrameBuffer = NULL;
		}

		if(imageConvertContext != NULL) {

			sws_freeContext(imageConvertContext);
			imageConvertContext = NULL;
		}

		if(audioConvertContext != NULL) {

			swr_free(&audioConvertContext);
			audioConvertContext = NULL;
		}

		if(videoFilterGraph != NULL) {

			delete videoFilterGraph;
			videoFilterGraph = NULL;
		}

		if(audioFilterGraph != NULL) {

			delete audioFilterGraph;
			audioFilterGraph = NULL;
		}

		startTime = 0;
	
		closed = true;
	}

	std::string getFileName() const {

		return(formatContext->filename);
	}

	virtual void open(String ^location, AVDiscard discardMode = AVDISCARD_DEFAULT) {

		// Convert location to UTF8 string pointer
		array<Byte>^ encodedBytes = System::Text::Encoding::UTF8->GetBytes(location);

		// prevent GC moving the bytes around while this variable is on the stack
		pin_ptr<Byte> pinnedBytes = &encodedBytes[0];

		// Call the function, typecast from byte* -> char* is required
		char *locationUTF8 = reinterpret_cast<char*>(pinnedBytes);

		int errorCode = 0;

		if((errorCode = avformat_open_input(&formatContext, locationUTF8, NULL, NULL)) != 0)
		{			
			throw gcnew VideoLib::VideoLibException("Unable to open the stream:" + VideoInit::errorToString(errorCode));
		}

		// generate pts?? -- from ffplay, not documented
		// it should make av_read_frame() generate pts for unknown value
		formatContext->flags |= AVFMT_FLAG_GENPTS;

		if((errorCode = avformat_find_stream_info(formatContext, NULL)) < 0)
		{		
			throw gcnew VideoLib::VideoLibException("Unable to find the stream's info: " + VideoInit::errorToString(errorCode));
		}

		for(unsigned int i = 0; i < formatContext->nb_streams; i++) {

			AVCodec *decoder = NULL;

			if(formatContext->streams[i]->codec->codec_type == AVMediaType::AVMEDIA_TYPE_VIDEO ||
				formatContext->streams[i]->codec->codec_type == AVMediaType::AVMEDIA_TYPE_AUDIO)
			{
				decoder = avcodec_find_decoder(formatContext->streams[i]->codec->codec_id);
				if(decoder == NULL)
				{		
					throw gcnew VideoLib::VideoLibException("Unsupported decoder for input stream");
				}
			}

			VideoLib::Stream *newStream = new VideoLib::Stream(formatContext->streams[i], decoder);			
		
			stream.push_back(newStream);			
		}

		videoIdx = av_find_best_stream(formatContext, AVMEDIA_TYPE_VIDEO, -1, -1, NULL, 0);
		if(videoIdx < 0) {
					
			throw gcnew VideoLib::VideoLibException("Unable to find a video stream");
		}

		stream[videoIdx]->open();
		stream[videoIdx]->getCodecContext()->skip_frame = discardMode; 	
	
		audioIdx = av_find_best_stream(formatContext, AVMEDIA_TYPE_AUDIO, -1, -1, NULL, 0);
		if(audioIdx >= 0) {
				
			stream[audioIdx]->open();
		}
			
		frame = avcodec_alloc_frame();
		if(frame == NULL)
		{		
			throw gcnew VideoLib::VideoLibException("Unable to allocate frame memory");
		}

		if(formatContext->start_time == AV_NOPTS_VALUE) {

			startTime = 0;

		} else {

			startTime = formatContext->start_time / AV_TIME_BASE;
		}

		durationSeconds  = formatContext->duration / AV_TIME_BASE;
		
		if(durationSeconds < 0) {

			durationSeconds  = getVideoStream()->duration * av_q2d(getVideoStream()->time_base);
		}
		
		if(durationSeconds < 0) {
		
			throw gcnew VideoLib::VideoLibException("can't determine video duration");
		}
		
		av_init_packet(&packet);
		packet.data = NULL;
		packet.size = 0;

		// int64_t duration_tb = duration / av_q2d(pStream->time_base); 
		closed = false;

		sizeBytes = formatContext->pb ? avio_size(formatContext->pb) : 0;

		if(hasAudio()) {

			samplesPerSecond = getAudioCodecContext()->sample_rate;
			bytesPerSample = av_get_bytes_per_sample(getAudioCodecContext()->sample_fmt);
			
			nrChannels = getAudioCodecContext()->channels;
		}		

	}

	int getSizeBytes() const {

		return(sizeBytes);
	}

	int getAudioSamplesPerSecond() const {

		return(samplesPerSecond);
	}

	int getAudioBytesPerSample() const  {

		return(bytesPerSample);
	}

	int getAudioNrChannels() const {

		return(nrChannels);
	}

	int getAudioBytesPerSecond() const {

		return(samplesPerSecond * bytesPerSample * nrChannels);
	}

	double getDurationSeconds() const {
		
		return(durationSeconds);
	}

	SwsContext *getImageConvertContext() const {

		return(imageConvertContext);
	}

	AVPixelFormat getImageConvertFormat() const {

		return(imageConvertFormat);
	}

	SwrContext *getAudioConvertContext() const {

		return(audioConvertContext);
	}

	AVSampleFormat getAudioConvertFormat() const {

		return(audioConvertFormat);
	}

	FilterGraph *getVideoFilterGraph() const {

		return(videoFilterGraph);
	}

	FilterGraph *getAudioFilterGraph() const {

		return(audioFilterGraph);
	}

	bool decodeFrame(VideoDecodeMode videoMode = DECODE_VIDEO, 
		AudioDecodeMode audioMode = DECODE_AUDIO) 
	{

		if(isClosed()) return(false);

		int frameFinished = 0;

		while(!frameFinished) {

			if(av_read_frame(formatContext, &packet) != 0) {

				// cannot read frame or end of file
				return(false);
			}

			// only decode video/keyframe or non corrupt packets
			if((packet.stream_index == videoIdx) &&
				(videoMode != DECODE_KEY_FRAMES_ONLY || (packet.flags & AV_PKT_FLAG_KEY)) &&
				!(packet.flags & AV_PKT_FLAG_CORRUPT) &&
				(videoMode != SKIP_VIDEO))
			{

				avcodec_get_frame_defaults(frame);

				int ret = avcodec_decode_video2(getVideoCodecContext(), frame, &frameFinished, &packet);
				if(ret < 0) {

					//Error decoding video frame
					//return(0);
				}

				if(frameFinished)
				{		
					AVFrame *finishedFrame = frame;					

					if(imageConvertContext != NULL) {

						sws_scale(imageConvertContext,
							frame->data,
							frame->linesize,
							0,
							frame->height,
							convertedFrame->data,
							convertedFrame->linesize);

						finishedFrame = convertedFrame;
					}

					if(decodedFrame != NULL) {
						decodedFrame(data, &packet, finishedFrame, VIDEO);
					}

				}

			} else if((packet.stream_index == audioIdx) &&
				(audioMode == DECODE_AUDIO)) {

					avcodec_get_frame_defaults(frame);

					int ret = avcodec_decode_audio4(getAudioCodecContext(), frame, &frameFinished, 
						&packet);

					if(ret < 0) {
						//Error decoding audio frame
						//return(0);
					}

					if(frameFinished)
					{		
						if(decodedFrame != NULL) {
							decodedFrame(data, &packet, frame, AUDIO);				
						}
					}
			}

			av_free_packet(&packet);
		}

		return(true);

	}


	bool seek(double posSeconds) {

		// convert timestamp into a videostream timestamp
		AVRational myAVTIMEBASEQ = {1, AV_TIME_BASE}; 
	
		int64_t seekTarget = posSeconds / av_q2d(myAVTIMEBASEQ);
		
		int ret = av_seek_frame(formatContext, -1, seekTarget, 0);
		if(ret >= 0) { 

			avcodec_flush_buffers(getVideoCodecContext());

			if(hasAudio()) {

				avcodec_flush_buffers(getAudioCodecContext());
			}

			return true;
		}

		return false;
		
	}

	void initImageConverter(AVPixelFormat format, int dstWidth, int dstHeight, 
		SamplingMode sampling, bool useFilterGraph = false) 
	{

		if(useFilterGraph == false) {

			convertedFrame = avcodec_alloc_frame();
			if(convertedFrame == NULL)
			{
				throw gcnew VideoLib::VideoLibException("Unable to allocate frame memory");
			}

			int numBytes = avpicture_get_size(format, dstWidth, dstHeight);
			convertedFrameBuffer = (uint8_t*)av_malloc(numBytes);

			avpicture_fill((AVPicture*)convertedFrame, convertedFrameBuffer, format, dstWidth, dstHeight);

			imageConvertContext = sws_getContext(
				getVideoCodecContext()->width,
				getVideoCodecContext()->height,
				getVideoCodecContext()->pix_fmt,
				dstWidth,
				dstHeight,
				format,
				sampling,
				NULL,
				NULL,
				NULL);

			if(imageConvertContext == NULL) {

				throw gcnew VideoLib::VideoLibException("Unable to allocate video convert context");

			} else {

				imageConvertFormat = format;
			}

		} else {
			
			videoFilterGraph = new FilterGraph(getVideoCodecContext(), format, sampling);
			videoFilterGraph->createGraph("null");
		}
		
	}

	bool initAudioConverter(int sampleRate = 44100, int64_t channelLayout = AV_CH_LAYOUT_STEREO, 
		AVSampleFormat sampleFormat = AV_SAMPLE_FMT_S16, bool useFilterGraph = false) 
	{

		if(hasAudio() == false) {

			return(false);
		}

		if(useFilterGraph == false) {

			this->audioConvertFormat = sampleFormat;

			// Note that AVCodecContext::channel_layout may or may not be set by libavcodec. Because of this,
			// we won't use it, and will instead try to guess the layout from the number of channels.
			audioConvertContext = swr_alloc_set_opts(NULL,
				channelLayout,
				sampleFormat,
				sampleRate,
				av_get_default_channel_layout(getAudioCodecContext()->channels),
				getAudioCodecContext()->sample_fmt,
				getAudioCodecContext()->sample_rate,
				0,
				NULL);

			if(audioConvertContext == NULL) {

				throw gcnew VideoLib::VideoLibException("Unable to allocate audio convert context");
			}

			if(swr_init(audioConvertContext) != 0)
			{
				throw gcnew VideoLib::VideoLibException("Unable to initialize audio convert context");
			}

			return(true);

		} else {

			audioFilterGraph = new FilterGraph(getAudioCodecContext(), sampleFormat, channelLayout, sampleRate);
			audioFilterGraph->createGraph("anull");

			return(true);
		}
	}

	int getWidth() const {

		return(hasVideo() ? getVideoCodecContext()->width : 0);
	}

	int getHeight() const {

		return(hasVideo() ? getVideoCodecContext()->height : 0);
	}


	void convertVideoFrame(AVFrame *input, AVFrame *output)
	{

		if(imageConvertContext != NULL) {

			// convert frame to the right format
			sws_scale(imageConvertContext,
				input->data,
				input->linesize,
				0,
				input->height,
				output->data,
				output->linesize);

		} else {

			videoFilterGraph->filterFrame(input, output);
		}
	}
	
};

}