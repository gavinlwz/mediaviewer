#pragma once

namespace imageviewer {

using namespace System;
using namespace System::IO;
using namespace System::Text::RegularExpressions;
using namespace System::Collections::Generic;


public ref class Util 
{

public: 
	
	static void DebugOut(String ^string) {

		System::Diagnostics::Debug::Print(string);
	}

	static bool isUrl(String ^string) 
	{

			if(string->StartsWith(L"http://") || string->StartsWith(L"https://")) {

				return(true);

			} else {

				return(false);
			}
	}

	static String ^getProperDirectoryCapitalization(DirectoryInfo ^dirInfo)
	{
		DirectoryInfo ^parentDirInfo = dirInfo->Parent;
		if(parentDirInfo == nullptr) {

			return dirInfo->Name;
		}

		return Path::Combine(getProperDirectoryCapitalization(parentDirInfo),
			parentDirInfo->GetDirectories(dirInfo->Name)[0]->Name);
	}

	static String ^getProperFilePathCapitalization(String ^filename)
	{
		FileInfo ^fileInfo = gcnew FileInfo(filename);
		DirectoryInfo ^dirInfo = fileInfo->Directory;

		String ^result = Path::Combine(getProperDirectoryCapitalization(dirInfo),
			dirInfo->GetFiles(fileInfo->Name)[0]->Name);

		return(Char::ToUpper(result[0]) + result->Substring(1));
	}


	static String ^getPathWithoutFileName(String ^fullPath) {

		String ^fileName = System::IO::Path::GetFileName(fullPath);

		if(String::IsNullOrEmpty(fileName)) return(fullPath);

		return(fullPath->Remove(fullPath->Length - fileName->Length - 1));
	}

	static String ^removeIllegalCharsFromFileName(String  ^fileName) {

		String ^regexSearch = gcnew String(Path::GetInvalidFileNameChars()) + gcnew String(Path::GetInvalidPathChars());
		Regex ^r = gcnew Regex(String::Format("[{0}]", Regex::Escape(regexSearch)));
		return( r->Replace(fileName, "") );

	}

	generic <typename T>
	static bool listSortAndCompare(List<T> ^a, List<T> ^b) {

		if(a->Count != b->Count) return(false);

		a->Sort();
		b->Sort();

		for(int i = 0; i < a->Count; i++) {

			if(!a[i]->Equals(b[i])) {
				
				return(false);
			}
		}

		return(true);
	}

};

}