// imageviewer.cpp : main project file.

#include "stdafx.h"
#include "MainForm.h"

using namespace imageviewer;

[STAThreadAttribute]
int main(array<System::String ^> ^args)
{
	// Enabling Windows XP visual effects before any controls are created
	Application::EnableVisualStyles();
	Application::SetCompatibleTextRenderingDefault(false); 

	// Create the main window and run it
	Application::Run(gcnew MainForm(args));

	return 0;
}
