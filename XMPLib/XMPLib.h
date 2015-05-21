#pragma once

#include "XMPDLL.h"

////#include "MetaDataProperty.h"
////#include "MetaDataTree.h"

namespace XMPLib {

	#include "Consts.h"

	using namespace System;
	using namespace System::Collections::Generic;

	public ref class MetaDataProperty 
	{
	public:

		String ^nameSpace;
		String ^path;
		String ^value;

		MetaDataProperty();
		MetaDataProperty(const MetaDataProperty ^p);

		virtual bool Equals(Object ^obj) override;

	};

	public ref class VersionInfo
	{
	public:

		int build;
		int flags;
		bool isDebug;
		String ^message;
		int major;
		int micro;
		int minor;
	};

	public ref class MetaData
	{

	public:

		enum class LogLevel 
		{
			ERROR,
			WARNING,
			INFO
		};

		delegate void LogCallbackDelegate(LogLevel level, String ^message);			
		delegate bool ErrorCallbackDelegate(String ^filePath, Byte errorSeverity, System::UInt32 cause, String ^message);			
		delegate bool ProgressCallbackDelegate(float elapsedTime, float fractionDone, float secondsToGo);	

	private:

		XMPDLL::XMPFile *xmpFile;

		static LogCallbackDelegate ^logCallback;

		static void log(LogLevel level, String ^message);

		GCHandle errorCallbackPtr, progressCallbackPtr;
			
		[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
		delegate bool NativeErrorCallbackDelegate(void *context, XMP_StringPtr filePath, XMP_ErrorSeverity errorSeverity, XMP_Int32 cause,  XMP_StringPtr message);
		bool errorCallback(void *context, XMP_StringPtr filePath, XMP_ErrorSeverity errorSeverity, XMP_Int32 cause,  XMP_StringPtr message);

		[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
		delegate bool NativeProgressCallbackDelegate(void *context, float secondsDone, float fractionsDone, float secondsToGo);
		bool progressCallback(void *context, float elapsedTime, float fractionDone, float secondsToGo);


		ErrorCallbackDelegate ^managedErrorCallback;
		ProgressCallbackDelegate ^managedProgressCallback;

		static void WStringToUTF8(String ^input, std::string &output);
		static void UTF8ToWString(const std::string &input, String ^%output);

	public:

		static MetaData() {

			logCallback = nullptr;
		}
	
		MetaData(ErrorCallbackDelegate ^errorCallback, ProgressCallbackDelegate ^progress);
		~MetaData();

		bool open(String ^filename, Consts::OpenOptions options); 

		bool dumpToDisk(String ^filename);

		bool canPutXMP();
		void putXMP();

		bool doesPropertyExists(String ^nameSpace, String ^propName);
		void deleteProperty(String ^nameSpace, String ^propName);

		void getProperty(String ^nameSpace, String ^propName, String^ %propValue);		
		void getProperty_Date(String ^nameSpace, String ^propName, Nullable<DateTime> %propValue);
		void getProperty_Bool(String ^nameSpace, String ^propName, Nullable<bool> %propValue);
		void getProperty_Float(String ^nameSpace, String ^propName, Nullable<double> %propValue);
		void getProperty_Int(String ^nameSpace, String ^propName, Nullable<long> %propValue);
		void getProperty_Int64(String ^nameSpace, String ^propName, Nullable<Int64> %propValue);

		void setProperty(String ^nameSpace, String ^propName, String ^propValue, Consts::PropOptions options);
		void setProperty_Date(String ^nameSpace, String ^propName, DateTime propValue);
		void setProperty_Bool(String ^nameSpace, String ^propName, bool propValue);
		void setProperty_Float(String ^nameSpace, String ^propName, double propValue);
		void setProperty_Int(String ^nameSpace, String ^propName, long propValue);
		void setProperty_Int64(String ^nameSpace, String ^propName, Int64 propValue);

		int countArrayItems(String ^nameSpace, String ^arrayName);
		void getArrayItem(String ^nameSpace, String ^arrayName, int item, String^ %itemValue);
		bool doesArrayItemExist(String ^nameSpace, String ^arrayName, int item);
		void setArrayItem(String ^nameSpace, String ^arrayName, int item, String ^itemValue, Consts::PropOptions options);
		void appendArrayItem(String ^nameSpace, String ^arrayName, Consts::PropOptions arrayOptions, String ^itemValue, Consts::PropOptions options);
		void deleteArrayItem(String ^nameSpace, String ^arrayName, int item);

		bool getStructField(String ^nameSpace, String ^structName, String ^fieldNameSpace, 
			String ^fieldName, String ^%fieldValue);
		void setStructField(String ^nameSpace, String ^structName, String ^fieldNameSpace, 
			String ^fieldName, String ^fieldValue, XMP_OptionBits options);
		void deleteStructField(String ^nameSpace, String ^structName, String ^fieldNameSpace, 
			String ^fieldName);

		void getLocalizedText(String ^nameSpace, String ^textName, String ^genericLang,  String ^specificLang, String ^ %itemValue);
		void setLocalizedText(String ^nameSpace, String ^textName, String ^genericLang, String ^specificLang, String ^itemValue);
		
		////MetaDataTreeNode ^parse();

		void iterate(Consts::IterOptions options, List<MetaDataProperty ^> ^%properties);
		void iterate(String ^nameSpace, List<MetaDataProperty ^> ^%properties);
		void iterate(String ^nameSpace, Consts::IterOptions options, List<MetaDataProperty ^> ^%properties);
		
		static DateTime convertToDate(String ^dateString);

		static void composeArrayItemPath(String ^nameSpace, 
			String ^arrayName, 
			int itemIndex, 
			String ^%fullPath);

		static void composeStructFieldPath(String ^nameSpace, 
			String ^structName, 
			String ^fieldNameSpace, 
			String ^fieldName, 
			String ^%fullPath);

		static void encodeToBase64(String ^rawStr,
			String ^%encodedStr); 

		static void decodeFromBase64(String ^encodedStr,
			String ^%rawStr); 

		static void setLogCallback(LogCallbackDelegate ^callback);

		static void getVersionInfo(VersionInfo ^%info);
	};
}
