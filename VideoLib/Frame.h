#pragma once

namespace VideoLib {

	public ref class Frame 
	{
	public:

		enum class FrameType {

			AUDIO,
			VIDEO
		};

	protected:

		AVFrame *avFrame;

		double pts;
		FrameType frameType;

	public:

		property AVFrame *AVLibFrameData
		{
			AVFrame *get() {

				return(avFrame);
			}			
		}

		Frame(FrameType frameType) {

			avFrame = avcodec_alloc_frame();
			if(avFrame == NULL) {

				throw gcnew VideoLibException("Cannot allocate Frame");
			}

			this->frameType = frameType;
			pts = 0;
		}

		!Frame() {

			if(avFrame != NULL) {

				av_free(avFrame);
				avFrame = NULL;
			}
		}

		~Frame() {

			this->!Frame();
		}

		property double Pts {

			void set(double pts) {
				this->pts = pts;
			}

			double get() {
				return(pts);
			}
		}

		property FrameType FrameTypeP
		{
			FrameType get() {

				return(frameType);
			}

		}

	};
}