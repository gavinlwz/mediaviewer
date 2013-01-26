#pragma once

#include "MediaFileFactory.h"
#include "AnimatedPictureBoxControl.h"
#include "TransparentIconPanel.h"

using namespace System;
using namespace System::ComponentModel;
using namespace System::Collections;
using namespace System::Windows::Forms;
using namespace System::Data;
using namespace System::Drawing;
using namespace System::Drawing::Imaging;


namespace imageviewer {

	/// <summary>
	/// Summary for MediaPreviewControl
	/// </summary>
	public ref class MediaPreviewControl : public System::Windows::Forms::UserControl
	{
	public:
		MediaPreviewControl(void)
		{
			InitializeComponent();
			//
			//TODO: Add the constructor code here
			//
			mediaFileFactory = gcnew MediaFileFactory();
			mediaFileFactory->OpenFinished += gcnew EventHandler<MediaFile ^>(this, &MediaPreviewControl::mediaFileFactory_OpenFinished);
			media = nullptr;

			caption = "";

			informImage = gcnew List<Image ^>();
			informImage->Add(gcnew Bitmap("C:\\game\\icons\\loading.gif"));
			informImage->Add(gcnew Bitmap("c:\\game\\icons\\error.png"));
			
		}

	protected:
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		~MediaPreviewControl()
		{
			if (components)
			{
				delete components;
			}
		}
	private: System::Windows::Forms::Panel^  panel;
	protected: 

	private: System::Windows::Forms::ToolTip^  toolTip;
	private: System::Windows::Forms::ImageList^  imageList;

	private: imageviewer::AnimatedPictureBoxControl^  pictureBox;




	private: System::ComponentModel::IContainer^  components;

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
			this->panel = (gcnew System::Windows::Forms::Panel());
			this->toolTip = (gcnew System::Windows::Forms::ToolTip(this->components));
			this->pictureBox = (gcnew imageviewer::AnimatedPictureBoxControl());
			this->panel->SuspendLayout();
			this->SuspendLayout();
			// 
			// panel
			// 
			this->panel->Controls->Add(this->pictureBox);
			this->panel->Dock = System::Windows::Forms::DockStyle::Fill;
			this->panel->Location = System::Drawing::Point(0, 0);
			this->panel->Name = L"panel";
			this->panel->Size = System::Drawing::Size(336, 348);
			this->panel->TabIndex = 0;
			// 
			// pictureBox
			// 
			this->pictureBox->Dock = System::Windows::Forms::DockStyle::Fill;
			this->pictureBox->Image = nullptr;
			this->pictureBox->Location = System::Drawing::Point(0, 0);
			this->pictureBox->LowerColor = System::Drawing::Color::FromArgb(static_cast<System::Int32>(static_cast<System::Byte>(200)), static_cast<System::Int32>(static_cast<System::Byte>(200)), 
				static_cast<System::Int32>(static_cast<System::Byte>(200)));
			this->pictureBox->Name = L"pictureBox";
			this->pictureBox->Size = System::Drawing::Size(336, 348);
			this->pictureBox->SizeMode = System::Windows::Forms::PictureBoxSizeMode::Zoom;
			this->pictureBox->TabIndex = 0;
			this->pictureBox->TransparencyEnabled = false;
			this->pictureBox->UpperColor = System::Drawing::Color::FromArgb(static_cast<System::Int32>(static_cast<System::Byte>(255)), static_cast<System::Int32>(static_cast<System::Byte>(255)), 
				static_cast<System::Int32>(static_cast<System::Byte>(255)));
			this->pictureBox->MouseDown += gcnew System::Windows::Forms::MouseEventHandler(this, &MediaPreviewControl::pictureBox_MouseDown);
			// 
			// MediaPreviewControl
			// 
			this->AutoScaleDimensions = System::Drawing::SizeF(9, 20);
			this->AutoScaleMode = System::Windows::Forms::AutoScaleMode::Font;
			this->Controls->Add(this->panel);
			this->Name = L"MediaPreviewControl";
			this->Size = System::Drawing::Size(336, 348);
			this->panel->ResumeLayout(false);
			this->ResumeLayout(false);

		}
#pragma endregion

private:

	enum class InformImage {

		LOADING_IMAGE = 0,
		ERROR_IMAGE = 1,		
	};
	
	MediaFileFactory ^mediaFileFactory;
	MediaFile ^media;
	List<Image ^> ^informImage;
	
	String ^caption;

	delegate void loadPreviewDelegate(MediaFile ^media, List<MetaDataThumb ^> ^thumbs);

	void mediaFileFactory_OpenFinished(System::Object ^sender, MediaFile ^media) {

		array<Object ^> ^args = gcnew array<Object ^>(2);

		args[0] = media;
		args[1] = gcnew List<MetaDataThumb ^>();

		try {

			// grab or generate thumbnail images
			List<MetaDataThumb ^> ^thumbs = nullptr;

			if(media->MetaData != nullptr && media->MetaData->Thumbnail->Count > 0) {

				thumbs = media->MetaData->Thumbnail;

			} else {

				thumbs = media->generateThumbnails();
			}

			args[1] = thumbs;			

		} catch (Exception ^e) {

			media->OpenError = e;

		} finally {

			this->Invoke(gcnew loadPreviewDelegate(this, &MediaPreviewControl::loadPreview), args);		
		}

	}
	
	void loadPreview(MediaFile ^media, List<MetaDataThumb ^> ^thumbs) {

		try {

			this->media = media;

			clearPictureBox();

			if(!media->OpenSuccess) {
			
				if(media->OpenError->GetType() != MediaFileException::typeid) {
					setPictureBoxInformImage(InformImage::ERROR_IMAGE);
				}

			} else if(String::IsNullOrEmpty(media->Location) || media->MediaFormat == MediaFile::MediaType::UNKNOWN) {
			
				return;
				
			} else if(thumbs->Count > 0) {

				setPictureBoxImage(thumbs[0]->ThumbImage);

				pictureBox->addInfoIcon(mimeTypeInfoIcon(media->MimeType));

				if(media->MetaData == nullptr) {

					pictureBox->addInfoIcon(0);
				}
			} 

			if(String::IsNullOrEmpty(caption)) {

				setToolTip(media->getDefaultCaption());

			} else {

				setToolTip(caption);			
			}

			
		} catch(Exception ^) {

			
		} finally {

			media->close();

			// release the lock on opening of images
			mediaFileFactory->releaseNonBlockingOpenLock();
		}

	}

	void setToolTip(String ^text) {

		toolTip->SetToolTip(pictureBox, text);
		//toolTip->SetToolTip(transparentIconPanel, text);
	}

	void setPictureBoxImage(Image ^image) {

		pictureBox->SizeMode = PictureBoxSizeMode::Zoom;
		pictureBox->TransparencyEnabled = false;
		pictureBox->Image = image;

	}

	void setPictureBoxInformImage(InformImage image) {

		clearPictureBox();

		pictureBox->SizeMode = PictureBoxSizeMode::CenterImage;
		pictureBox->TransparencyEnabled = true;
		pictureBox->Image = informImage[(int)image];

		
	}

	void clearPictureBox() {

		if(pictureBox->Image != nullptr) {

			if(!informImage->Contains(pictureBox->Image)) {

				delete pictureBox->Image;
			}
			pictureBox->Image = nullptr;
			pictureBox->clearInfoIcons();
			setToolTip("");
			
		}		
	}

	int mimeTypeInfoIcon(String ^mimeType) {

		if(mimeType->Equals("image/tiff")) {

			return(10);

		} else if(mimeType->Equals("image/gif")) {

			return(7);

		} else if(mimeType->Equals("image/png")) {

			return(9);

		} else if(mimeType->Equals("image/jpeg")) {

			return(8);

		} else if(mimeType->Equals("image/bmp")) {

			return(6);

		} else if(mimeType->Equals("video/x-ms-asf")) {

			return(5);
		
		} else if(mimeType->Equals("video/x-ms-wmv")) {

			return(4);
		
		} else if(mimeType->Equals("video/x-flv")) {

			return(3);
		
		} else if(mimeType->Equals("video/avi") || 
			mimeType->Equals("video/vnd.avi") ||
			mimeType->Equals("video/msvideo") ||
			mimeType->Equals("video/x-msvideo")) 
		{
		
			return(1);

		} else if(mimeType->Equals("video/mp4")) {

			return(3);
		
		} else if(mimeType->Equals("video/quicktime")) {

			return(2);

		} else if(mimeType->Equals("video/x-matroska")) {

			return(3);

		} else if(mimeType->Equals("video/x-m4v")) {

			return(3);

		} else {

			return(8);
		}

	}


protected:



public:

	event EventHandler<MouseEventArgs ^> ^PreviewMouseDown;
	event EventHandler<MouseEventArgs ^> ^PreviewMouseDoubleClick;

	enum class DisplayMode {
		NORMAL,
		THUMBNAIL

	};

	property String ^ToolTip {

		void set(String ^caption) {
			
			this->caption = caption;
		}

	}

	property String ^Location {

		String ^get() {

			if(media == nullptr) return("");
			else return(media->Location);
		}

	}

	property bool IsEmpty {

		bool get() {

			return(pictureBox->Image == nullptr ? true : false);
		}

	}

	void loadMedia(String ^fileLocation, DisplayMode mode) {
		
		if(String::IsNullOrEmpty(fileLocation)) {

			clearPictureBox();

		} else {

			setPictureBoxInformImage(InformImage::LOADING_IMAGE);
		}
		//pictureBox->Image = miscImageList->Images[0];

		mediaFileFactory->openNonBlockingAndCancelPending(fileLocation, mode);

	}



private: System::Void pictureBox_MouseDown(System::Object^  sender, System::Windows::Forms::MouseEventArgs^  e) {

			 PreviewMouseDown(this, e);
			 

		 }



};
}
