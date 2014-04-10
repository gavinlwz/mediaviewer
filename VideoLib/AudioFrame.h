#pragma once

#include "Frame.h"
#include "VideoDecoder.h"

#define AVCODEC_MAX_AUDIO_FRAME_SIZE 192000 

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::IO;

namespace VideoLib {

	public ref class AudioFrame : public Frame
	{
	private:
	
		MemoryStream ^stream;
		array<unsigned char> ^data;
		int length;

		bool hasAllocatedOwnBuffers;

	public:


		AudioFrame() : Frame(FrameType::AUDIO) {

			data = gcnew array<unsigned char>(AVCODEC_MAX_AUDIO_FRAME_SIZE * 2);
			stream = gcnew MemoryStream(data);
			length = 0;
		
			hasAllocatedOwnBuffers = false;
		}

		// only packed audio types should be used as format
		AudioFrame(AVSampleFormat format) :
			Frame(FrameType::AUDIO)
		{

			data = gcnew array<unsigned char>(AVCODEC_MAX_AUDIO_FRAME_SIZE * 2);
			stream = gcnew MemoryStream(data);
			length = 0;

			// planar types should allocate more data planes
			// here
			AVLibFrameData->data[0] = (uint8_t *)av_malloc(AVCODEC_MAX_AUDIO_FRAME_SIZE * 2);
			AVLibFrameData->format = format;
		
			hasAllocatedOwnBuffers = true;
		}

		!AudioFrame() {

			if(hasAllocatedOwnBuffers == true && avFrame->data[0] != NULL) {

				av_free(avFrame->data[0]);
				avFrame->data[0] = NULL;
			}

		}

		~AudioFrame() {

			if(stream != nullptr) {

				delete stream;
				stream = nullptr;
			}

			this->!AudioFrame();
		}

		property MemoryStream ^Stream {

			MemoryStream ^get() {

				return(stream);
			}

		}

		property array<unsigned char> ^Data {

			array<unsigned char> ^get() {

				return(data);
			}

		}

		property int Length {

			int get() {

				return(length);
			}

			void set(int length) {

				this->length = length;
			}

		}

		void copyAudioDataToManagedMemory()
		{

			if(length > 0) {

				this->length = length;			
				Marshal::Copy(IntPtr(AVLibFrameData->data[0]), data, 0, length);

			} else {

				// incorrect audio, set to silence
				this->length = 4608;
				Array::Clear(data,0,data->Length);

			}

			stream->Position = 0;
			
		}
	};
}