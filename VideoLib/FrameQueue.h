#pragma once

#ifdef __cplusplus
#define __STDC_CONSTANT_MACROS
#ifdef _STDINT_H
#undef _STDINT_H
#endif
# include "stdint.h"
#endif

#include "PacketQueue.h"
#include "VideoFrame.h"
#include "AudioFrame.h"
#include "Packet.h"
#include "VideoDecoder.h"

using namespace System::Collections::Generic;
using namespace System::Threading;
using namespace System::Diagnostics;

namespace VideoLib {

	public ref class FrameQueue
	{
	private:

		VideoDecoder *videoDecoder;

		VideoFrame ^videoFrame, ^convertedVideoFrame;
		AudioFrame ^audioFrame, ^convertedAudioFrame;

		static const int maxVideoPackets = 300;
		static const int maxAudioPackets = 300;

		PacketQueue ^freePackets;
		PacketQueue ^videoPackets;
		PacketQueue ^audioPackets;

		array<WaitHandle ^> ^decodingPaused;
	
		array<Packet ^> ^packetData;

		double videoClock;
		double audioClock;

		bool audioPacketsClosed;
		bool videoPacketsClosed;

		double synchronizeVideo(int repeatFrame, __int64 dts) {

			double pts;

			if(dts != AV_NOPTS_VALUE) {

				// convert pts to seconds
				pts = dts * av_q2d(videoDecoder->getVideoStream()->time_base);
				// set clock to current pts;
				videoClock = pts;

			} else {

				// if we aren't given a pts, set it to the clock 
				pts = videoClock;
			}

			// update the video clock to the pts of the next frame
			double frameDelay = av_q2d(videoDecoder->getVideoStream()->time_base);
			// if we are repeating a frame, adjust clock accordingly 
			frameDelay += repeatFrame * (frameDelay * 0.5);
			videoClock += frameDelay;

			return(pts);
		}

		double synchronizeAudio(int sizeBytes, __int64 dts) {

			double pts;

			if(dts != AV_NOPTS_VALUE) {

				// convert pts to seconds
				pts = dts * av_q2d(videoDecoder->getAudioStream()->time_base);
				// set clock to current pts;
				audioClock = pts;

			} else {

				// if we aren't given a pts, set it to the clock 
				pts = audioClock;
				// calculate next pts in seconds
				audioClock += sizeBytes / double(videoDecoder->getAudioBytesPerSecond());
			}

			return(pts);
		}

		void videoPackets_Finished(Object ^sender, EventArgs ^) {

			videoPacketsClosed = true;
			if(audioPacketsClosed == true) {

				// send framequeue finished event when and only when both 
				// audio and video packet queues are closed
				freePackets->stop();

				Finished(this, EventArgs::Empty);
				videoPacketsClosed = false;
				audioPacketsClosed = false;
			}
		}

		void audioPackets_Finished(Object ^sender, EventArgs ^) {

			audioPacketsClosed = true;
			if(videoPacketsClosed == true) {

				// send framequeue finished event when and only when both 
				// audio and video packet queues are closed
				freePackets->stop();

				Finished(this, EventArgs::Empty);
				videoPacketsClosed = false;
				audioPacketsClosed = false;
			}
		}

	public:

		event EventHandler ^Finished;

		FrameQueue(VideoDecoder *videoDecoder) {

			this->videoDecoder = videoDecoder;

			videoFrame = nullptr;
			audioFrame = nullptr;

			convertedVideoFrame = nullptr;
			convertedAudioFrame = nullptr;

			packetData = gcnew array<Packet ^>(maxVideoPackets + maxAudioPackets);

			for(int i = 0; i < packetData->Length; i++) {

				packetData[i] = gcnew Packet();
			}

			freePackets = gcnew PacketQueue("freePackets", maxVideoPackets + maxAudioPackets);
			videoPackets = gcnew PacketQueue("videoPackets", maxVideoPackets);
			audioPackets = gcnew PacketQueue("audioPackets", maxAudioPackets);

			decodingPaused = gcnew array<WaitHandle^> {
				audioPackets->Paused, videoPackets->Paused}; 		

			videoPackets->Finished += gcnew EventHandler(this, &FrameQueue::videoPackets_Finished);
			audioPackets->Finished += gcnew EventHandler(this, &FrameQueue::audioPackets_Finished);

			audioPacketsClosed = false;
			videoPacketsClosed = false;
		
		}

		~FrameQueue() {

			release();

			for(int i = 0; i < packetData->Length; i++) {

				delete packetData[i];
			}

			if(freePackets != nullptr) {

				delete freePackets;
				freePackets = nullptr;
			}

			if(videoPackets != nullptr) {

				delete videoPackets;
				videoPackets = nullptr;
			}

			if(audioPackets != nullptr) {

				delete audioPackets;
				audioPackets = nullptr;
			}

		}

		property int MaxVideoPackets {

			int get() {

				return(maxVideoPackets);
			}
		}

		property int MaxAudioPackets {

			int get() {

				return(maxAudioPackets);
			}
		}

		property int VideoPacketsInQueue {

			int get() {

				return(videoPackets->QueueSize);
			}
		}

		property int AudioPacketsInQueue {

			int get() {

				return(audioPackets->QueueSize);
			}
		}


		void initialize() {

			release();

			videoClock = 0;
			audioClock = 0;

			videoFrame = gcnew VideoFrame();

			convertedVideoFrame = gcnew VideoFrame(videoDecoder->getWidth(), 
				videoDecoder->getHeight(), videoDecoder->getImageConvertFormat());
			
			if(videoDecoder->hasAudio()) {

				audioFrame = gcnew AudioFrame();

				convertedAudioFrame = gcnew AudioFrame(
					videoDecoder->getAudioConvertFormat());
			}

			for(int i = 0; i < packetData->Length; i++) {

				freePackets->add(packetData[i]);
			}

		}

		void pause() {

			videoPackets->pause();
			audioPackets->pause();

			WaitHandle::WaitAll(decodingPaused);
		}

		void flush() {		

			// note that demuxing is not active during this function
			videoPackets->flush();
			audioPackets->flush();
			freePackets->flush();

			for(int i = 0; i < packetData->Length; i++) {

				addFreePacket(packetData[i]);
			}
		
		}

		void start() {

			freePackets->open();
			videoPackets->open();
			audioPackets->open();

		}

		void stop() {

			Video::writeToLog(AV_LOG_DEBUG,"Frame Queue Stop called");

			// stop each queue in turn and wait until the threads signal they have actually stopped
			if(freePackets->QueueState != PacketQueue::State::STOPPED) {						
				freePackets->stop();				
				freePackets->Stopped->WaitOne();		
				Video::writeToLog(AV_LOG_DEBUG,"freePackets stopped");
			}
			if(videoPackets->QueueState != PacketQueue::State::STOPPED) {				
				videoPackets->stop();
				videoPackets->Stopped->WaitOne();	
				Video::writeToLog(AV_LOG_DEBUG,"videoPackets stopped");
			}
			if(audioPackets->QueueState != PacketQueue::State::STOPPED) {			
				audioPackets->stop();
				audioPackets->Stopped->WaitOne();		
				Video::writeToLog(AV_LOG_DEBUG,"audioPackets stopped");
			}

			Video::writeToLog(AV_LOG_DEBUG,"Frame Queue Stop finished");
		}
	
		void release() {

			if(convertedVideoFrame != nullptr) {

				delete convertedVideoFrame;
				convertedVideoFrame = nullptr;
			}

			if(convertedAudioFrame != nullptr) {

				delete convertedAudioFrame;
				convertedAudioFrame = nullptr;
			}

			if(videoFrame != nullptr) {

				delete videoFrame;
				videoFrame = nullptr;
			}

			if(audioFrame != nullptr) {

				delete audioFrame;
				audioFrame = nullptr;
			}

			videoPackets->flush();
			audioPackets->flush();
			freePackets->flush();

			for(int i = 0; i < packetData->Length; i++) {

				if(packetData[i] != nullptr) {

					packetData[i]->free();
				}

			}

		}

		bool getFreePacket(Packet ^%packet) {

			bool result = freePackets->tryGet(packet);

			return(result);
		}

		void addFreePacket(Packet ^packet) {

			// free packet data before inserting it back into freepackets
			packet->free();
			freePackets->add(packet);
		}

		void addVideoPacket(Packet ^packet) {

			videoPackets->add(packet);
		}

		void addAudioPacket(Packet ^packet) {

			audioPackets->add(packet);
		}

		// allow one video frame to decode while the queue is in 
/*		// a blocked state
		void decodeNextVideoFrame() {

			ThreadSafeQueue<Packet ^>::State videoQueueState = videoPackets->QueueState;

			if(videoQueueState == ThreadSafeQueue<Packet ^>::State::OPEN) {

				return;
			}

			videoFrameDecoded->Reset();
			videoQueueState->open();			
			videoFrameDecoded->WaitOne();

			// ISSUE: cannot guarantee that only one frame passes trough ..
			if(videoQueueState == ThreadSafeQueue<Packet ^>::State::STOPPED) {

				videoQueue->stop();

			} else {

				videoQueue->pause();
			}

		}
*/
		VideoFrame ^getDecodedVideoFrame() {

			int frameFinished = 0;

			while(!frameFinished) {

				Packet ^videoPacket = nullptr;

				bool success = videoPackets->tryGet(videoPacket);
				if(success == false) {
			
					return(nullptr);
				}

				avcodec_get_frame_defaults(videoFrame->AVLibFrameData);

				int ret = avcodec_decode_video2(videoDecoder->getVideoCodecContext(), 
					videoFrame->AVLibFrameData, &frameFinished, videoPacket->AVLibPacketData);
				if(ret < 0) {

					Video::writeToLog(AV_LOG_WARNING, "could not decode video frame");
				}

				if(frameFinished)
				{

					// convert frame to the right format
					sws_scale(videoDecoder->getImageConvertContext(),
						videoFrame->AVLibFrameData->data,
						videoFrame->AVLibFrameData->linesize,
						0,
						videoFrame->AVLibFrameData->height,
						convertedVideoFrame->AVLibFrameData->data,
						convertedVideoFrame->AVLibFrameData->linesize);

					convertedVideoFrame->Pts = synchronizeVideo(
						videoFrame->AVLibFrameData->repeat_pict, 
						videoPacket->AVLibPacketData->dts);
				}

				addFreePacket(videoPacket);
			}


			return(convertedVideoFrame);


		}


		AudioFrame ^getDecodedAudioFrame() {

			int frameFinished = 0;		

			while(!frameFinished) {

				Packet ^audioPacket = nullptr;

				bool success = audioPackets->tryGet(audioPacket);
				if(success == false) {

					return(nullptr);
				}

				avcodec_get_frame_defaults(audioFrame->AVLibFrameData);

				int ret = avcodec_decode_audio4(videoDecoder->getAudioCodecContext(), 
					audioFrame->AVLibFrameData, &frameFinished, audioPacket->AVLibPacketData);
						
				if(ret < 0) {

					Video::writeToLog(AV_LOG_WARNING, "could not decode audio frame");
				}

				if(frameFinished)
				{

					SwrContext *acc = videoDecoder->getAudioConvertContext();

					// convert audio to a packed format ready for playback
					int numSamplesOut = swr_convert(acc,
							convertedAudioFrame->AVLibFrameData->data,
                            audioFrame->AVLibFrameData->nb_samples,
                            (const unsigned char**)audioFrame->AVLibFrameData->extended_data,
                            audioFrame->AVLibFrameData->nb_samples);

					// audio length does not equal linesize, because some extra 
					// padding bytes may be added for alignment.
					// Instead av_samples_get_buffer_size needs to be used
					convertedAudioFrame->Length = av_samples_get_buffer_size(NULL, 
						videoDecoder->getAudioNrChannels(), 
						numSamplesOut,
						(AVSampleFormat)convertedAudioFrame->AVLibFrameData->format, 
						1);
				
					convertedAudioFrame->Pts = synchronizeAudio(audioFrame->Length, 
						audioPacket->AVLibPacketData->dts);

					convertedAudioFrame->copyAudioDataToManagedMemory();

				}

				addFreePacket(audioPacket);
			}


			return(convertedAudioFrame);


		}


	};
}