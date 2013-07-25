#pragma once
// Tutorial for building a video player from ffmpeg: http://dranger.com/ffmpeg/tutorial05.html
// Directx9 tutorial: http://www.drunkenhyena.com/cgi-bin/dx9_net.pl
// Implementing videoRender in directx: http://www.codeproject.com/Articles/207642/Video-Shadering-with-Direct3D
// Note that there are 4 threads in use during video playback
// 1) The main UI thread
// 2) The video decoding thread
// 3) The video render thread
// 4) The audio playback thread
#include "ImageUtils.h"
#include "Util.h"
#include "WindowsUtils.h"
#include "HRTimerFactory.h"
#include "StreamingAudioBuffer.h"
#include "VideoRender.h"
#include "VideoDebugForm.h"
#include "VideoState.h"
#include "CustomToolTip.h"
#include "Settings.h"
#include <stdio.h>
#include <string.h>

using namespace System;
using namespace System::ComponentModel;
using namespace System::Collections;
using namespace System::Windows::Forms;
using namespace System::Data;
using namespace System::Drawing;
using namespace VideoLib;
using namespace Diagnostics;
namespace D3D = SharpDX::Direct3D9;
namespace DS = SharpDX::DirectSound;


namespace imageviewer {

	/// <summary>
	/// Summary for VideoPanelControl
	/// </summary>
	public ref class VideoPanelControl : public System::Windows::Forms::UserControl
	{
	public:
		VideoPanelControl(void)
		{
			InitializeComponent();
			//
			//TODO: Add the constructor code here
			//
			//videoRender = nullptr;
			//mediaPlayer->Dock = DockStyle::Fill;
			//mediaPlayer->stretchToFit = true;


			videoRender = gcnew VideoRender(VideoPanel);
			audioPlayer = gcnew StreamingAudioBuffer(this);

			//videoRender->initialize(0,0);
			videoDecoder = gcnew VideoPlayer();	
			videoDecoder->FrameQueue->Closed += gcnew EventHandler(this, &VideoPanelControl::frameQueue_Closed);

			videoDecoder->setLogCallback(gcnew VideoPlayer::LogCallbackDelegate(this, &VideoPanelControl::videoDecoderLogCallback),
				true, true);
			
			videoRefreshTimer = HRTimerFactory::create(HRTimerFactory::TimerType::TIMER_QUEUE);
			videoRefreshTimer->Tick += gcnew EventHandler(this, &VideoPanelControl::videoRefreshTimer_Tick);
			//videoRefreshTimer->SynchronizingObject = this;
			videoRefreshTimer->AutoReset = false;

			audioRefreshTimer = HRTimerFactory::create(HRTimerFactory::TimerType::TIMER_QUEUE);
			audioRefreshTimer->Tick += gcnew EventHandler(this, &VideoPanelControl::audioRefreshTimer_Tick);
			audioRefreshTimer->AutoReset = false;
			audioRefreshTimer->SynchronizingObject = nullptr;

			videoDebug = gcnew VideoDebugForm();

			audioDiffAvgCoef  = Math::Exp(Math::Log(0.01) / AUDIO_DIFF_AVG_NB);

			//syncMode = SyncMode::VIDEO_SYNCS_TO_AUDIO;
			syncMode = SyncMode::AUDIO_SYNCS_TO_VIDEO;
			VideoState = imageviewer::VideoState::CLOSED;

			updateTimeTrackBar = true;

			timeTrackBarToolTip = gcnew CustomToolTip();
			timeTrackBarToolTip->BackColor = SystemColors::Info;
			this->Controls->Add(timeTrackBarToolTip);

			timeTrackBarToolTip->Show();
			timeTrackBarToolTip->BringToFront();
			timeTrackBarToolTip->Visible = false;			
			
			muteCheckBox->Checked = bool::Parse(Settings::getVar(Settings::VarName::VIDEO_MUTED));
			volumeTrackBar->Value = Util::lerp<int>(Double::Parse(Settings::getVar(Settings::VarName::VIDEO_VOLUME)), volumeTrackBar->Minimum, volumeTrackBar->Maximum);

		}

	protected:
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		~VideoPanelControl()
		{
			if (components)
			{
				delete components;
			}
		}
	private: System::ComponentModel::BackgroundWorker^  videoDecoderBW;
	protected: 


	private: System::Windows::Forms::SplitContainer^  splitContainer;
	private: System::Windows::Forms::TrackBar^  timeTrackBar;
	private: System::Windows::Forms::Button^  stopButton;

	private: System::Windows::Forms::CheckBox^  debugVideoCheckBox;
	private: System::Windows::Forms::TrackBar^  volumeTrackBar;
	private: System::Windows::Forms::CheckBox^  muteCheckBox;
	private: System::Windows::Forms::ToolTip^  toolTip1;
	private: System::Windows::Forms::ImageList^  imageList;
	private: System::Windows::Forms::Label^  videoTimeLabel;
	private: System::Windows::Forms::CheckBox^  playCheckBox;
	private: System::Windows::Forms::Button^  nextButton;
	private: System::Windows::Forms::Button^  prevButton;
	private: System::Windows::Forms::Button^  screenShotButton;



























	private: System::ComponentModel::IContainer^  components;
	protected: 

	protected: 

	protected: 

	protected: 

	private:
		/// <summary>
		/// Required designer variable.
		/// </summary>


#pragma region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		void InitializeComponent(void)
		{
			this->components = (gcnew System::ComponentModel::Container());
			System::ComponentModel::ComponentResourceManager^  resources = (gcnew System::ComponentModel::ComponentResourceManager(VideoPanelControl::typeid));
			this->videoDecoderBW = (gcnew System::ComponentModel::BackgroundWorker());
			this->splitContainer = (gcnew System::Windows::Forms::SplitContainer());
			this->screenShotButton = (gcnew System::Windows::Forms::Button());
			this->imageList = (gcnew System::Windows::Forms::ImageList(this->components));
			this->prevButton = (gcnew System::Windows::Forms::Button());
			this->nextButton = (gcnew System::Windows::Forms::Button());
			this->playCheckBox = (gcnew System::Windows::Forms::CheckBox());
			this->videoTimeLabel = (gcnew System::Windows::Forms::Label());
			this->muteCheckBox = (gcnew System::Windows::Forms::CheckBox());
			this->volumeTrackBar = (gcnew System::Windows::Forms::TrackBar());
			this->debugVideoCheckBox = (gcnew System::Windows::Forms::CheckBox());
			this->stopButton = (gcnew System::Windows::Forms::Button());
			this->timeTrackBar = (gcnew System::Windows::Forms::TrackBar());
			this->toolTip1 = (gcnew System::Windows::Forms::ToolTip(this->components));
			(cli::safe_cast<System::ComponentModel::ISupportInitialize^  >(this->splitContainer))->BeginInit();
			this->splitContainer->Panel2->SuspendLayout();
			this->splitContainer->SuspendLayout();
			(cli::safe_cast<System::ComponentModel::ISupportInitialize^  >(this->volumeTrackBar))->BeginInit();
			(cli::safe_cast<System::ComponentModel::ISupportInitialize^  >(this->timeTrackBar))->BeginInit();
			this->SuspendLayout();
			// 
			// videoDecoderBW
			// 
			this->videoDecoderBW->WorkerReportsProgress = true;
			this->videoDecoderBW->WorkerSupportsCancellation = true;
			this->videoDecoderBW->DoWork += gcnew System::ComponentModel::DoWorkEventHandler(this, &VideoPanelControl::videoDecoderBW_DoWork);
			// 
			// splitContainer
			// 
			this->splitContainer->BorderStyle = System::Windows::Forms::BorderStyle::Fixed3D;
			this->splitContainer->Dock = System::Windows::Forms::DockStyle::Fill;
			this->splitContainer->FixedPanel = System::Windows::Forms::FixedPanel::Panel2;
			this->splitContainer->IsSplitterFixed = true;
			this->splitContainer->Location = System::Drawing::Point(0, 0);
			this->splitContainer->Name = L"splitContainer";
			this->splitContainer->Orientation = System::Windows::Forms::Orientation::Horizontal;
			// 
			// splitContainer.Panel2
			// 
			this->splitContainer->Panel2->Controls->Add(this->screenShotButton);
			this->splitContainer->Panel2->Controls->Add(this->prevButton);
			this->splitContainer->Panel2->Controls->Add(this->nextButton);
			this->splitContainer->Panel2->Controls->Add(this->playCheckBox);
			this->splitContainer->Panel2->Controls->Add(this->videoTimeLabel);
			this->splitContainer->Panel2->Controls->Add(this->muteCheckBox);
			this->splitContainer->Panel2->Controls->Add(this->volumeTrackBar);
			this->splitContainer->Panel2->Controls->Add(this->debugVideoCheckBox);
			this->splitContainer->Panel2->Controls->Add(this->stopButton);
			this->splitContainer->Panel2->Controls->Add(this->timeTrackBar);
			this->splitContainer->Size = System::Drawing::Size(800, 484);
			this->splitContainer->SplitterDistance = 388;
			this->splitContainer->TabIndex = 0;
			// 
			// screenShotButton
			// 
			this->screenShotButton->ImageIndex = 7;
			this->screenShotButton->ImageList = this->imageList;
			this->screenShotButton->Location = System::Drawing::Point(231, 38);
			this->screenShotButton->MaximumSize = System::Drawing::Size(60, 51);
			this->screenShotButton->Name = L"screenShotButton";
			this->screenShotButton->Size = System::Drawing::Size(51, 40);
			this->screenShotButton->TabIndex = 11;
			this->toolTip1->SetToolTip(this->screenShotButton, L"Take A Screenshot");
			this->screenShotButton->UseVisualStyleBackColor = true;
			this->screenShotButton->Click += gcnew System::EventHandler(this, &VideoPanelControl::screenShotButton_Click);
			// 
			// imageList
			// 
			this->imageList->ImageStream = (cli::safe_cast<System::Windows::Forms::ImageListStreamer^  >(resources->GetObject(L"imageList.ImageStream")));
			this->imageList->TransparentColor = System::Drawing::Color::Transparent;
			this->imageList->Images->SetKeyName(0, L"1361908321_1777.ico");
			this->imageList->Images->SetKeyName(1, L"1361908340_1776.ico");
			this->imageList->Images->SetKeyName(2, L"Button Play-01.ico");
			this->imageList->Images->SetKeyName(3, L"Button Pause-01.ico");
			this->imageList->Images->SetKeyName(4, L"Button Turn Off-01.ico");
			this->imageList->Images->SetKeyName(5, L"Button Next-01.ico");
			this->imageList->Images->SetKeyName(6, L"Button Previous-01.ico");
			this->imageList->Images->SetKeyName(7, L"Computer Monitor-01.ico");
			// 
			// prevButton
			// 
			this->prevButton->ImageIndex = 6;
			this->prevButton->ImageList = this->imageList;
			this->prevButton->Location = System::Drawing::Point(117, 37);
			this->prevButton->MaximumSize = System::Drawing::Size(60, 51);
			this->prevButton->Name = L"prevButton";
			this->prevButton->Size = System::Drawing::Size(51, 40);
			this->prevButton->TabIndex = 10;
			this->toolTip1->SetToolTip(this->prevButton, L"Previous Video");
			this->prevButton->UseVisualStyleBackColor = true;
			this->prevButton->Click += gcnew System::EventHandler(this, &VideoPanelControl::prevButton_Click);
			// 
			// nextButton
			// 
			this->nextButton->ImageIndex = 5;
			this->nextButton->ImageList = this->imageList;
			this->nextButton->Location = System::Drawing::Point(174, 37);
			this->nextButton->MaximumSize = System::Drawing::Size(60, 51);
			this->nextButton->Name = L"nextButton";
			this->nextButton->Size = System::Drawing::Size(51, 40);
			this->nextButton->TabIndex = 9;
			this->toolTip1->SetToolTip(this->nextButton, L"Next Video");
			this->nextButton->UseVisualStyleBackColor = true;
			this->nextButton->Click += gcnew System::EventHandler(this, &VideoPanelControl::nextButton_Click);
			// 
			// playCheckBox
			// 
			this->playCheckBox->Appearance = System::Windows::Forms::Appearance::Button;
			this->playCheckBox->ImageIndex = 2;
			this->playCheckBox->ImageList = this->imageList;
			this->playCheckBox->Location = System::Drawing::Point(3, 38);
			this->playCheckBox->Margin = System::Windows::Forms::Padding(0, 3, 3, 3);
			this->playCheckBox->MaximumSize = System::Drawing::Size(51, 40);
			this->playCheckBox->Name = L"playCheckBox";
			this->playCheckBox->Size = System::Drawing::Size(51, 40);
			this->playCheckBox->TabIndex = 8;
			this->playCheckBox->TextAlign = System::Drawing::ContentAlignment::MiddleCenter;
			this->toolTip1->SetToolTip(this->playCheckBox, L"Play / Pause");
			this->playCheckBox->UseVisualStyleBackColor = true;
			this->playCheckBox->CheckedChanged += gcnew System::EventHandler(this, &VideoPanelControl::playCheckBox_CheckedChanged);
			// 
			// videoTimeLabel
			// 
			this->videoTimeLabel->AutoSize = true;
			this->videoTimeLabel->Dock = System::Windows::Forms::DockStyle::Right;
			this->videoTimeLabel->Location = System::Drawing::Point(515, 37);
			this->videoTimeLabel->MaximumSize = System::Drawing::Size(137, 36);
			this->videoTimeLabel->MinimumSize = System::Drawing::Size(0, 36);
			this->videoTimeLabel->Name = L"videoTimeLabel";
			this->videoTimeLabel->Size = System::Drawing::Size(137, 36);
			this->videoTimeLabel->TabIndex = 7;
			this->videoTimeLabel->Text = L"00:00:00/00:00:00";
			this->videoTimeLabel->TextAlign = System::Drawing::ContentAlignment::MiddleRight;
			// 
			// muteCheckBox
			// 
			this->muteCheckBox->Appearance = System::Windows::Forms::Appearance::Button;
			this->muteCheckBox->Dock = System::Windows::Forms::DockStyle::Right;
			this->muteCheckBox->ImageIndex = 0;
			this->muteCheckBox->ImageList = this->imageList;
			this->muteCheckBox->Location = System::Drawing::Point(652, 37);
			this->muteCheckBox->MaximumSize = System::Drawing::Size(40, 36);
			this->muteCheckBox->Name = L"muteCheckBox";
			this->muteCheckBox->Size = System::Drawing::Size(40, 36);
			this->muteCheckBox->TabIndex = 6;
			this->muteCheckBox->TextAlign = System::Drawing::ContentAlignment::MiddleCenter;
			this->toolTip1->SetToolTip(this->muteCheckBox, L"Mute");
			this->muteCheckBox->UseVisualStyleBackColor = true;
			this->muteCheckBox->CheckedChanged += gcnew System::EventHandler(this, &VideoPanelControl::muteCheckBox_CheckedChanged);
			// 
			// volumeTrackBar
			// 
			this->volumeTrackBar->AutoSize = false;
			this->volumeTrackBar->Dock = System::Windows::Forms::DockStyle::Right;
			this->volumeTrackBar->Location = System::Drawing::Point(692, 37);
			this->volumeTrackBar->Maximum = 100;
			this->volumeTrackBar->MaximumSize = System::Drawing::Size(104, 36);
			this->volumeTrackBar->Name = L"volumeTrackBar";
			this->volumeTrackBar->Size = System::Drawing::Size(104, 36);
			this->volumeTrackBar->TabIndex = 5;
			this->volumeTrackBar->TickStyle = System::Windows::Forms::TickStyle::None;
			this->toolTip1->SetToolTip(this->volumeTrackBar, L"Volume");
			this->volumeTrackBar->ValueChanged += gcnew System::EventHandler(this, &VideoPanelControl::volumeTrackBar_ValueChanged);
			// 
			// debugVideoCheckBox
			// 
			this->debugVideoCheckBox->Appearance = System::Windows::Forms::Appearance::Button;
			this->debugVideoCheckBox->Location = System::Drawing::Point(288, 38);
			this->debugVideoCheckBox->Name = L"debugVideoCheckBox";
			this->debugVideoCheckBox->Size = System::Drawing::Size(65, 40);
			this->debugVideoCheckBox->TabIndex = 4;
			this->debugVideoCheckBox->Text = L"Debug";
			this->debugVideoCheckBox->TextAlign = System::Drawing::ContentAlignment::MiddleCenter;
			this->debugVideoCheckBox->UseVisualStyleBackColor = true;
			this->debugVideoCheckBox->CheckedChanged += gcnew System::EventHandler(this, &VideoPanelControl::debugVideoCheckBox_CheckedChanged);
			// 
			// stopButton
			// 
			this->stopButton->ImageIndex = 4;
			this->stopButton->ImageList = this->imageList;
			this->stopButton->Location = System::Drawing::Point(60, 37);
			this->stopButton->MaximumSize = System::Drawing::Size(60, 51);
			this->stopButton->Name = L"stopButton";
			this->stopButton->Size = System::Drawing::Size(51, 40);
			this->stopButton->TabIndex = 2;
			this->toolTip1->SetToolTip(this->stopButton, L"Stop");
			this->stopButton->UseVisualStyleBackColor = true;
			this->stopButton->Click += gcnew System::EventHandler(this, &VideoPanelControl::stopButton_Click);
			// 
			// timeTrackBar
			// 
			this->timeTrackBar->AutoSize = false;
			this->timeTrackBar->Dock = System::Windows::Forms::DockStyle::Top;
			this->timeTrackBar->Location = System::Drawing::Point(0, 0);
			this->timeTrackBar->Maximum = 5000;
			this->timeTrackBar->Name = L"timeTrackBar";
			this->timeTrackBar->Size = System::Drawing::Size(796, 37);
			this->timeTrackBar->TabIndex = 0;
			this->timeTrackBar->TickStyle = System::Windows::Forms::TickStyle::None;
			this->timeTrackBar->MouseDown += gcnew System::Windows::Forms::MouseEventHandler(this, &VideoPanelControl::timeTrackBar_MouseDown);
			this->timeTrackBar->MouseEnter += gcnew System::EventHandler(this, &VideoPanelControl::timeTrackBar_MouseEnter);
			this->timeTrackBar->MouseLeave += gcnew System::EventHandler(this, &VideoPanelControl::timeTrackBar_MouseLeave);
			this->timeTrackBar->MouseMove += gcnew System::Windows::Forms::MouseEventHandler(this, &VideoPanelControl::timeTrackBar_MouseMove);
			this->timeTrackBar->MouseUp += gcnew System::Windows::Forms::MouseEventHandler(this, &VideoPanelControl::timeTrackBar_MouseUp);
			// 
			// VideoPanelControl
			// 
			this->AutoScaleDimensions = System::Drawing::SizeF(9, 20);
			this->AutoScaleMode = System::Windows::Forms::AutoScaleMode::Font;
			this->Controls->Add(this->splitContainer);
			this->DoubleBuffered = true;
			this->Name = L"VideoPanelControl";
			this->Size = System::Drawing::Size(800, 484);
			this->splitContainer->Panel2->ResumeLayout(false);
			this->splitContainer->Panel2->PerformLayout();
			(cli::safe_cast<System::ComponentModel::ISupportInitialize^  >(this->splitContainer))->EndInit();
			this->splitContainer->ResumeLayout(false);
			(cli::safe_cast<System::ComponentModel::ISupportInitialize^  >(this->volumeTrackBar))->EndInit();
			(cli::safe_cast<System::ComponentModel::ISupportInitialize^  >(this->timeTrackBar))->EndInit();
			this->ResumeLayout(false);

		}
#pragma endregion
	private:

		static log4net::ILog ^log = log4net::LogManager::GetLogger(System::Reflection::MethodBase::GetCurrentMethod()->DeclaringType);

		VideoPlayer ^videoDecoder;
		VideoRender ^videoRender;
		StreamingAudioBuffer ^audioPlayer;

		VideoDebugForm ^videoDebug;

		// no AV sync correction is done if below the AV sync threshold 
		static const double AV_SYNC_THRESHOLD = 0.01;
		// no AV sync correction is done if too big error 
		static const double AV_NOSYNC_THRESHOLD = 10.0;

		static const double AUDIO_SAMPLE_CORRECTION_PERCENT_MAX = 10;

		// we use about AUDIO_DIFF_AVG_NB A-V differences to make the average 
		static const int AUDIO_DIFF_AVG_NB = 5;//20;

		enum class SyncMode {

			AUDIO_SYNCS_TO_VIDEO,
			VIDEO_SYNCS_TO_AUDIO

		} syncMode;

		VideoState videoState;

		double previousVideoPts;
		double previousVideoDelay;

		double videoFrameTimer;
		double audioFrameTimer;

		HRTimer ^videoRefreshTimer;		
		HRTimer ^audioRefreshTimer;

		double videoPts;
		double videoPtsDrift;

		double audioDiffCum;
		double audioDiffAvgCoef;
		double audioDiffThreshold;
		int audioDiffAvgCount;

		bool seekRequest;
		double seekPosition;

		bool updateTimeTrackBar;
		CustomToolTip ^timeTrackBarToolTip;

		delegate void UpdateUIDelegate(double, double, int);

		property Control ^VideoPanel {

			Control ^get() {

				return(splitContainer->Panel1);
			}
		}

		

		void invokeUpdateUI() {

			int curTime = (int)Math::Floor(getVideoClock());
			int totalTime = videoDecoder->DurationSeconds;

			videoTimeLabel->Text = Util::formatTimeSeconds(curTime) + "/" + Util::formatTimeSeconds(totalTime);

			if(updateTimeTrackBar == true) {

				double pos = Util::invlerp<int>(curTime,0,totalTime);
				timeTrackBar->Value = Util::lerp<int>(pos, timeTrackBar->Minimum, timeTrackBar->Maximum);
			}
		}

		void updateUI() {

			if(this->InvokeRequired) {

				// do not block (important!)
				this->BeginInvoke(gcnew Action(this, &VideoPanelControl::invokeUpdateUI));

			} else {
		
				invokeUpdateUI();
			}
		}

		double getVideoClock() {

			if(VideoState == imageviewer::VideoState::PAUSED) {

				return(videoPts);

			} else {

				return(videoPtsDrift - HRTimer::getTimestamp());
			}
		}

		void processVideoFrame() {

			bool skipVideoFrame = false;

restartvideo:
			
			double actualDelay = 0.04;

			Rectangle scaledVideoRec = ImageUtils::stretchRectangle(
				Rectangle(0,0,videoDecoder->Width, videoDecoder->Height),
				videoRender->Canvas);

			Rectangle canvas = ImageUtils::centerRectangle(videoRender->Canvas,
				scaledVideoRec);

			// grab a decoded frame, returns false if the queue is stopped
			VideoFrame ^videoFrame = videoDecoder->FrameQueue->getDecodedVideoFrame();

			if(VideoState == imageviewer::VideoState::CLOSED && videoFrame == nullptr) {

				return;

			} else if(VideoState == imageviewer::VideoState::PLAYING) {

				videoPts = videoFrame->Pts;
				videoPtsDrift = videoFrame->Pts + HRTimer::getTimestamp();

				if(skipVideoFrame == false) {

					videoRender->display(videoFrame, canvas, Color::Black, VideoRender::RenderMode::NORMAL);
					videoDebug->VideoFrames = videoDebug->VideoFrames + 1;
				} 					

				actualDelay = synchronizeVideo(videoPts);					

			} else if(VideoState == imageviewer::VideoState::PAUSED) {

				videoRender->display(nullptr, canvas, Color::Black, VideoRender::RenderMode::PAUSED);			
			}

			// do not update ui elements on main thread inside videoStateLock
			// or we can get a deadlock
			videoDebug->update();
			updateUI();

			if(actualDelay < 0.010) {

				// delay is too small skip next frame
				skipVideoFrame = true;
				videoDebug->NrVideoFramesDropped = videoDebug->NrVideoFramesDropped + 1;
				goto restartvideo;

			} 

			// start timer with delay for next frame
			videoRefreshTimer->Interval = int(actualDelay * 1000 + 0.5);
			videoRefreshTimer->start();		

		}


		double synchronizeVideo(double videoPts) {

			// assume delay to next frame equals delay between previous frames
			double delay = videoPts - previousVideoPts;	

			if(delay <= 0 || delay >= 1.0) {
				// if incorrect delay, use previous one 
				delay = previousVideoDelay;
			}

			previousVideoPts = videoPts;
			previousVideoDelay = delay;

			if(videoDecoder->HasAudio && syncMode == SyncMode::VIDEO_SYNCS_TO_AUDIO) {

				// synchronize video to audio
				double diff = getVideoClock() - audioPlayer->getAudioClock();

				// Skip or repeat the frame. Take delay into account
				// FFPlay still doesn't "know if this is the best guess."
				double sync_threshold = (delay > AV_SYNC_THRESHOLD) ? delay : AV_SYNC_THRESHOLD;

				if(Math::Abs(diff) < AV_NOSYNC_THRESHOLD) {

					if(diff <= -sync_threshold) {

						delay = 0;

					} else if(diff >= sync_threshold) {

						delay = 2 * delay;
					}
				}

			}

			// adjust delay based on the actual current time
			videoFrameTimer += delay;
			double actualDelay = videoFrameTimer - HRTimer::getTimestamp();

			videoDebug->VideoDelay = delay;
			videoDebug->ActualVideoDelay = actualDelay;
			videoDebug->VideoSync = getVideoClock();
			videoDebug->AudioSync = audioPlayer->getAudioClock();
			videoDebug->VideoQueueSize = videoDecoder->FrameQueue->VideoPacketsInQueue;
			videoDebug->AudioQueueSize = videoDecoder->FrameQueue->AudioPacketsInQueue;
			

			return(actualDelay);
		}


		void processAudioFrame() {

restartaudio:
			
			AudioFrame ^audioFrame = videoDecoder->FrameQueue->getDecodedAudioFrame();
			if(audioFrame == nullptr) return;

			videoDebug->AudioFrames = videoDebug->AudioFrames + 1;
			videoDebug->AudioFrameLength = audioFrame->Length;

			// if the audio is lagging behind too much, skip the buffer completely
			double diff = getVideoClock() - audioFrame->Pts;
			if(diff > 0.2 && diff < 3 && syncMode == SyncMode::AUDIO_SYNCS_TO_VIDEO) {

				log->Warn("dropping audio buffer, lagging behind: " + (getVideoClock() - audioFrame->Pts).ToString() + " seconds");
				goto restartaudio;
			}

			//adjustAudioSamplesPerSecond(audioFrame);
			adjustAudioLength(audioFrame);

			audioPlayer->write(audioFrame);

			int frameLength = audioFrame->Length;

			double actualDelay = synchronizeAudio(frameLength);

			if(actualDelay < 0) {

				// delay too small, play next frame as quickly as possible
				videoDebug->NrAudioFramesLaggingBehind = videoDebug->NrAudioFramesLaggingBehind + 1;
				goto restartaudio;

			} 
		
			// start timer with delay for next frame
			audioRefreshTimer->Interval = int(actualDelay * 1000 + 0.5);
			audioRefreshTimer->start();

		}

		double synchronizeAudio(int frameLength) {

			// calculate delay to play next frame
			int bytesPerSecond = audioPlayer->SamplesPerSecond * 
				videoDecoder->BytesPerSample * videoDecoder->NrChannels;

			double delay = frameLength / double(bytesPerSecond);

			// adjust delay based on the actual current time
			audioFrameTimer += delay;
			double actualDelay = audioFrameTimer - HRTimer::getTimestamp();

			videoDebug->AudioDelay = delay;
			videoDebug->ActualAudioDelay = actualDelay;			

			return(actualDelay);
		}


		void adjustAudioSamplesPerSecond(AudioFrame ^frame) {

			videoDebug->AudioFrameLengthAdjust = 0;

			if(syncMode == SyncMode::AUDIO_SYNCS_TO_VIDEO) {

				int n = videoDecoder->NrChannels * videoDecoder->BytesPerSample;

				double diff = audioPlayer->getAudioClock() - getVideoClock();

				if(Math::Abs(diff) < AV_NOSYNC_THRESHOLD) {

					// accumulate the diffs
					audioDiffCum = diff + audioDiffAvgCoef * audioDiffCum;

					if(audioDiffAvgCount < AUDIO_DIFF_AVG_NB) {

						audioDiffAvgCount++;

					} else {

						double avgDiff = audioDiffCum * (1.0 - audioDiffAvgCoef);

						// Shrinking/expanding buffer code....
						if(Math::Abs(avgDiff) >= audioDiffThreshold) {

							int wantedSize = int(frame->Length + diff * videoDecoder->SamplesPerSecond * n);
								
							// get a correction percent from 10 to 60 based on the avgDiff
							// in order to converge a little faster
							double correctionPercent = Util::clamp<double>(10 + (Math::Abs(avgDiff) - audioDiffThreshold) * 15, 10, 60);

							//Util::DebugOut(correctionPercent);

							//AUDIO_SAMPLE_CORRECTION_PERCENT_MAX

							int minSize = int(frame->Length * ((100 - correctionPercent)
								/ 100));

							int maxSize = int(frame->Length * ((100 + correctionPercent) 
								/ 100));

							if(wantedSize < minSize) {

								wantedSize = minSize;

							} else if(wantedSize > maxSize) {

								wantedSize = maxSize;
							}

							// adjust samples per second to speed up or slow down the audio
							__int64 length = frame->Length;
							__int64 sps = videoDecoder->SamplesPerSecond;
							int samplesPerSecond = int((length * sps) / wantedSize);
							videoDebug->AudioFrameLengthAdjust = samplesPerSecond;
							audioPlayer->SamplesPerSecond = samplesPerSecond;
							
						} else {

							audioPlayer->SamplesPerSecond = videoDecoder->SamplesPerSecond;
						}

					}

				} else {

					// difference is TOO big; reset diff stuff 
					audioDiffAvgCount = 0;
					audioDiffCum = 0;
				}
			}
			
		}


		void adjustAudioLength(AudioFrame ^frame) {

			videoDebug->AudioFrameLengthAdjust = 0;

			if(syncMode == SyncMode::AUDIO_SYNCS_TO_VIDEO) {

				int n = videoDecoder->NrChannels * videoDecoder->BytesPerSample;

				double diff = audioPlayer->getAudioClock() - getVideoClock();

				if(Math::Abs(diff) < AV_NOSYNC_THRESHOLD) {

					// accumulate the diffs
					audioDiffCum = diff + audioDiffAvgCoef * audioDiffCum;

					if(audioDiffAvgCount < AUDIO_DIFF_AVG_NB) {

						audioDiffAvgCount++;

					} else {

						double avgDiff = audioDiffCum * (1.0 - audioDiffAvgCoef);

						// Shrinking/expanding buffer code....
						if(Math::Abs(avgDiff) >= audioDiffThreshold) {

							int wantedSize = int(frame->Length + diff * videoDecoder->SamplesPerSecond * n);
								
							// get a correction percent from 10 to 60 based on the avgDiff
							// in order to converge a little faster
							double correctionPercent = Util::clamp<double>(10 + (Math::Abs(avgDiff) - audioDiffThreshold) * 15, 10, 60);

							//Util::DebugOut(correctionPercent);

							//AUDIO_SAMPLE_CORRECTION_PERCENT_MAX

							int minSize = int(frame->Length * ((100 - correctionPercent)
								/ 100));

							int maxSize = int(frame->Length * ((100 + correctionPercent) 
								/ 100));

							if(wantedSize < minSize) {

								wantedSize = minSize;

							} else if(wantedSize > maxSize) {

								wantedSize = maxSize;
							}

							// make sure the samples stay aligned after resizing the buffer
							wantedSize = (wantedSize / n) * n;

							if(wantedSize < frame->Length) {

								// remove samples 
								videoDebug->AudioFrameLengthAdjust = wantedSize - frame->Length;
								frame->Length = wantedSize;

							} else if(wantedSize > frame->Length) {
														
								// add samples by copying final samples
								int nrExtraSamples = wantedSize - frame->Length;
								videoDebug->AudioFrameLengthAdjust = nrExtraSamples;
						
								array<unsigned char> ^lastSample = gcnew array<unsigned char>(n);

								for(int i = 0; i < n; i++) {

									lastSample[i] = frame->Data[frame->Length - n + i];
								}

								frame->Stream->Position = frame->Length;

								while(nrExtraSamples > 0) {
									
									frame->Stream->Write(lastSample, 0, n);
									nrExtraSamples -= n;
								}

								frame->Stream->Position = 0;
								frame->Length = wantedSize;
							}

						}

					}

				} else {

					// difference is TOO big; reset diff stuff 
					audioDiffAvgCount = 0;
					audioDiffCum = 0;
				}
			}
			
		}

		void pausePlay() {

			if(VideoState == imageviewer::VideoState::PAUSED || 
				VideoState == imageviewer::VideoState::CLOSED) {
				
					return;
			}

			VideoState = imageviewer::VideoState::PAUSED;		

			videoDecoder->FrameQueue->stop();

			videoDecoderBW->CancelAsync();
	
			while(videoDecoderBW->IsBusy) {

				Application::DoEvents();
			}

			audioPlayer->stop();

		}

		void startPlay() {

			if(VideoState == imageviewer::VideoState::PLAYING || 
				VideoState == imageviewer::VideoState::CLOSED) {
					
				return;
			}

			VideoState = imageviewer::VideoState::PLAYING;

			audioPlayer->startPlayAfterNextWrite();

			videoDecoder->FrameQueue->start();
			
			videoDecoderBW->RunWorkerAsync();

			previousVideoPts = 0;
			previousVideoDelay = 0.04;

			audioDiffAvgCount = 0;

			videoRefreshTimer->start();
			audioRefreshTimer->start();
			
		}

		void invokeStopButtonClick() {

			stopButton->PerformClick();
		}

		void frameQueue_Closed(Object ^sender, EventArgs ^) {

			log->Info("Video stream end reached");
			this->BeginInvoke(gcnew Action(this, &VideoPanelControl::invokeStopButtonClick));
		}

		void fillFrameQueue() {

			bool success = true;

			while(videoDecoder->FrameQueue->AudioPacketsInQueue !=
				videoDecoder->FrameQueue->MaxAudioPackets &&
				videoDecoder->FrameQueue->VideoPacketsInQueue != 
				videoDecoder->FrameQueue->MaxVideoPackets &&
				success == true)
			{

				success = videoDecoder->demuxPacket();

			} 
			
		}

		void seek(double seconds) {

			seekPosition = seconds;
			seekRequest = true;			
		}

		void videoDecoderLogCallback(int level, String ^message) {

			if(level < 16) {

				log->Fatal(message);

			} else if(level == 16) {

				log->Error(message);

			} else if(level == 24) {

				log->Warn(message);

			} else {

				log->Info(message);
			}
		}


	public:

		property imageviewer::VideoState VideoState {

		private: 
			void set(imageviewer::VideoState videoState) {

				this->videoState = videoState;
			}

		public:
			imageviewer::VideoState get() {

				return(videoState);
			}
		}

		property String ^VideoLocation
		{

			String ^get() {

				return(videoDecoder->VideoLocation);
			}
		}

		void open(String ^location) {

			try {

				log->Info("Opening: " + location);

				close();
				videoDebug->clear();			

				videoDecoder->open(location);
				videoRender->initialize(videoDecoder->Width, videoDecoder->Height);

				if(videoDecoder->HasAudio) {

					audioPlayer->initialize(videoDecoder->SamplesPerSecond, videoDecoder->BytesPerSample,
						videoDecoder->NrChannels, videoDecoder->MaxAudioFrameSize * 2);			

					muteCheckBox->Enabled = true;
					volumeTrackBar->Enabled = true;

					audioDiffThreshold = 2.0 * 1024 / videoDecoder->SamplesPerSecond;

				} else {

					muteCheckBox->Enabled = false;
					volumeTrackBar->Enabled = false;
				}

				videoDebug->VideoQueueMaxSize = videoDecoder->FrameQueue->MaxVideoPackets;
				//videoDebug->VideoQueueSizeBytes = videoDecoder->FrameQueue->VideoQueueSizeBytes;	
				videoDebug->AudioQueueMaxSize = videoDecoder->FrameQueue->MaxAudioPackets;
				//videoDebug->AudioQueueSizeBytes = videoDecoder->FrameQueue->AudioQueueSizeBytes;

				if(syncMode == SyncMode::AUDIO_SYNCS_TO_VIDEO) {

					videoDebug->IsVideoSyncMaster = true;

				} else {

					videoDebug->IsAudioSyncMaster = true;
				}

				VideoState = imageviewer::VideoState::OPEN;

			} catch (VideoLibException ^e) {

				VideoState = imageviewer::VideoState::CLOSED;

				log->Error("Cannot open: " + location, e);
			
				MessageBox::Show("Cannot open: " + location + "\n\n" + 
					e->Message, "Video Error");

			}
		}

		void play() {

			if(playCheckBox->Checked == true && VideoState 
				!= imageviewer::VideoState::PLAYING) {

				startPlay();

			} else {

				playCheckBox->Checked = true;
			}
		}

		void pause() {

			playCheckBox->Checked = false;
		}
	
		void close() {

			if(VideoState == imageviewer::VideoState::CLOSED) {

				return;
			}

			VideoState = imageviewer::VideoState::CLOSED;

			videoDecoder->FrameQueue->stop();			

			videoDecoderBW->CancelAsync();
	
			while(videoDecoderBW->IsBusy) {

				Application::DoEvents();
			}
			
			videoDecoder->close();
			audioPlayer->flush();

			videoPts = 0;
			videoPtsDrift = 0;

			seekRequest = false;
			seekPosition = 0;

		}
	

private: System::Void videoDecoderBW_DoWork(System::Object^  sender, System::ComponentModel::DoWorkEventArgs^  e) {
			 			
				audioFrameTimer = videoFrameTimer = HRTimer::getTimestamp();

				bool success = true;

				// decode frames one by one, or handle seek requests
				do
				{
					if(seekRequest == true) {

						// wait for video and audio decoding to pause/block
						// To make sure no packets are in limbo
						// before flushing any ffmpeg internal or external queues. 
						videoDecoder->FrameQueue->pause();

						if(videoDecoder->seek(seekPosition) == true) {
							
							// flush the framequeue	and audioplayer buffer				
							videoDecoder->FrameQueue->flush();
							audioPlayer->flush();
							
							// refill/buffer the framequeue from the new position
							fillFrameQueue();
					
							audioFrameTimer = videoFrameTimer = HRTimer::getTimestamp();
							
						}
						seekRequest = false;

						// allow video and audio decoding to continue
						videoDecoder->FrameQueue->start();

					} else {

						success = videoDecoder->demuxPacket();
					}
									
				} while(success == true && !videoDecoderBW->CancellationPending);
				
		 }
private: System::Void videoRefreshTimer_Tick(System::Object^  sender, System::EventArgs^  e) {			
 
	
			 processVideoFrame();

		 }

private: System::Void audioRefreshTimer_Tick(Object^  sender, EventArgs ^e) {

			 processAudioFrame();
		 }

private: System::Void stopButton_Click(System::Object^  sender, System::EventArgs^  e) {

			 close();
			 videoRender->display(nullptr, Rectangle::Empty, this->BackColor, VideoRender::RenderMode::CLEAR_SCREEN);
			 timeTrackBar->Value = timeTrackBar->Minimum;
			 playCheckBox->Checked = false;
		 }

private: System::Void debugVideoCheckBox_CheckedChanged(System::Object^  sender, System::EventArgs^  e) {

			 if(debugVideoCheckBox->Checked == true) {

				videoDebug->Show();

			 } else {

				videoDebug->Hide();
			 }
		 }
private: System::Void volumeTrackBar_ValueChanged(System::Object^  sender, System::EventArgs^  e) {

			 double volume = Util::invlerp<int>(volumeTrackBar->Value,volumeTrackBar->Minimum, volumeTrackBar->Maximum);
			 audioPlayer->Volume = volume;
			 Settings::setVar(Settings::VarName::VIDEO_VOLUME, volume);
		 }
private: System::Void muteCheckBox_CheckedChanged(System::Object^  sender, System::EventArgs^  e) {

			 if(muteCheckBox->Checked == true) {

				 audioPlayer->Muted = true;
				 muteCheckBox->ImageIndex = 1;

			 } else {

				 audioPlayer->Muted = false;
				 muteCheckBox->ImageIndex = 0;

			 }

			 Settings::setVar(Settings::VarName::VIDEO_MUTED, muteCheckBox->Checked);
		 }
private: System::Void playCheckBox_CheckedChanged(System::Object^  sender, System::EventArgs^  e) {

			 if(playCheckBox->Checked == true) {

				 if(VideoState == imageviewer::VideoState::CLOSED && !String::IsNullOrEmpty(videoDecoder->VideoLocation)) {

					 open(videoDecoder->VideoLocation);					 
				 }

				 startPlay();
				 playCheckBox->ImageIndex = 3;
				 updateTimeTrackBar = true;

			 } else {

				 pausePlay();
				 playCheckBox->ImageIndex = 2;

			 }
		 }

private: System::Void timeTrackBar_MouseDown(System::Object^  sender, System::Windows::Forms::MouseEventArgs^  e) {

			 updateTimeTrackBar = false;

			 Rectangle chanRec = WindowsUtils::getTrackBarChannelRect(timeTrackBar);

			 double value = Util::invlerp<int>(e->X, chanRec.Left, chanRec.Right);

			 timeTrackBar->Value = Util::lerp<int>(value, timeTrackBar->Minimum, timeTrackBar->Maximum);
			
		 }
private: System::Void timeTrackBar_MouseUp(System::Object^  sender, System::Windows::Forms::MouseEventArgs^  e) {

			 int totalTime = videoDecoder->DurationSeconds;

			 Rectangle chanRec = WindowsUtils::getTrackBarChannelRect(timeTrackBar);

			 double value = Util::invlerp<int>(e->X, chanRec.Left, chanRec.Right);

			 int seconds = Util::lerp<int>(value, 0, totalTime);

			 seek(seconds);

			 if(VideoState != imageviewer::VideoState::PAUSED) {

				updateTimeTrackBar = true;
			 }
		
		 }

private: System::Void nextButton_Click(System::Object^  sender, System::EventArgs^  e) {
		 }
private: System::Void prevButton_Click(System::Object^  sender, System::EventArgs^  e) {
		 }


private: String ^timeTrackBarPosToTime(int mouseX) {

			 Rectangle chanRec = WindowsUtils::getTrackBarChannelRect(timeTrackBar);

			 double value = Util::invlerp<int>(mouseX, chanRec.Left, chanRec.Right);

			 int seconds = Util::lerp<int>(value, 0, videoDecoder->DurationSeconds);

			 return(Util::formatTimeSeconds(seconds));

		 }

private: System::Void timeTrackBar_MouseEnter(System::Object^  sender, System::EventArgs^  e) {
			 
			 if(VideoState == imageviewer::VideoState::CLOSED) return;

			 Point trackBarPos = this->PointToClient(timeTrackBar->PointToScreen(timeTrackBar->Location));

			 timeTrackBarToolTip->Location = Point(MousePosition.X - timeTrackBarToolTip->Width / 2, 
				 trackBarPos.Y - timeTrackBarToolTip->Height);
			 timeTrackBarToolTip->Text = timeTrackBarPosToTime(MousePosition.X);

			 timeTrackBarToolTip->Refresh();

			 timeTrackBarToolTip->Visible = true;
		 }

private: System::Void timeTrackBar_MouseLeave(System::Object^  sender, System::EventArgs^  e) {

			 timeTrackBarToolTip->Visible = false;
		 }
private: System::Void timeTrackBar_MouseMove(System::Object^  sender, System::Windows::Forms::MouseEventArgs^  e) {

			 Point trackBarPos = this->PointToClient(timeTrackBar->PointToScreen(timeTrackBar->Location));

			 timeTrackBarToolTip->Location = Point(e->X - timeTrackBarToolTip->Width / 2, 
				 trackBarPos.Y - timeTrackBarToolTip->Height);
			 timeTrackBarToolTip->Text = timeTrackBarPosToTime(e->X);

			 timeTrackBarToolTip->Refresh();
		 }
private: System::Void screenShotButton_Click(System::Object^  sender, System::EventArgs^  e) {

			 if(VideoState == imageviewer::VideoState::CLOSED) return;

			 //videoRender->setFullScreen();
			 videoRender->createScreenShot(videoDecoder->VideoLocation);
		 }
};
}
