using System;
using System.IO;
using System.Threading;
using Aliyun.OSS.Common;
using System.Text;
using Aliyun.OSS.Util;
using Aliyun.OSS;
using Newtonsoft.Json;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Collections;
using System.Security.Cryptography;
using CI.HttpClient.Core;
using GameNet;

namespace Utils
{
	public class OSS
	{
		static int tokenRemainTime = 60;
		static STSToken stsToken = new STSToken();
		static string endPoint = "oss-cn-beijing.aliyuncs.com";
//		static AutoResetEvent _event = new AutoResetEvent(false);
		private static IDispatcher _dispatcher;

		public static bool STSTokenIsValid()
		{
			if (stsToken != null) {
				double seconds = stsToken.Expiration.Subtract (DateTime.UtcNow).TotalSeconds;
				if (seconds < tokenRemainTime) {
					return false;
				}
				return true;
			}
			return false;
		}

		public static bool LoadSTSToken() {
			bool loaded = STSToken.Load<STSToken> ("STSToken.dat", ref stsToken);
			if (loaded) {
				loaded = STSTokenIsValid ();
			}
			return loaded;
		}
		public static void SaveSTSToken() {
			stsToken.Save ("STSToken.dat");
		}
		public static void FetchSTSToken(Action<STSToken> responseCallback = null, Action<string> errorCallback = null) {
			HttpMethod.Post(HttpConfig.STS, true, null, (r) =>
				{
					if(r.IsSuccessStatusCode){
						string response = r.Data;
						stsToken = (STSToken)JsonConvert.DeserializeObject (response, stsToken.GetType ());
						SaveSTSToken();
						if(responseCallback != null) {
							responseCallback(stsToken);
						}
					} else {
						if(errorCallback != null) {
							errorCallback(r.ReasonPhrase);
						}
					}
				});
		}

		public static void AsyncPutObject(string bucketName, string filename, Action<string> putCallback = null)
		{
			CreateDispatcherGameObject ();

			try
			{
				var fs = new FileStream (filename, FileMode.Open);
				string md5 = OssUtils.ComputeContentMd5(fs, fs.Length);

				var ossClient = new OssClient(endPoint, stsToken.AccessKeyId, stsToken.AccessKeySecret, stsToken.SecurityToken);
				var metadata = new ObjectMetadata();
				metadata.ContentLength = fs.Length;
				metadata.CacheControl = "public";
				metadata.ContentMd5 = md5;

				var filemd5 = Utils.File.FormatMD5(Convert.FromBase64String(md5));
				string ext = System.IO.Path.GetExtension(filename);
				var ossFileName = filemd5 + ext;

				Hashtable state = new Hashtable();
				state["client"] = ossClient;
				state["fs"] = fs;
				state["callback"] = putCallback;
				state["ossfilename"] = ossFileName;
				ossClient.BeginPutObject(bucketName, ossFileName, fs, metadata, PutObjectCallback, state);

//				_event.WaitOne();
			}
			catch (OssException ex)
			{
				Console.WriteLine("Failed with error code: {0}; Error info: {1}. \nRequestID:{2}\tHostID:{3}",
					ex.ErrorCode, ex.Message, ex.RequestId, ex.HostId);
				if(putCallback != null) {
					if (ex is OssException) {
						putCallback (((OssException)ex).ErrorCode);
					} else {
						putCallback (ex.Message);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Failed with error info: {0}", ex.Message);
				if(putCallback != null) {
					if (ex is ServiceException) {
						putCallback (((ServiceException)ex).ErrorCode);
					} else {
						putCallback (ex.Message);
					}
				}
			}
		}

		private static void PutObjectCallback(IAsyncResult ar)
		{
			Hashtable state = (Hashtable)ar.AsyncState;
			Action<string> putCallback = (Action<string>)state["callback"];
			try
			{
				OssClient ossClient = (OssClient)state["client"];
				FileStream fs = (FileStream)state["fs"];
				string ossfilename = (string)state["ossfilename"];
				ossClient.EndPutObject(ar);
				fs.Close();
				_dispatcher.Enqueue(() => {
					if(putCallback != null) {
						putCallback(ossfilename);
					}
				});
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				_dispatcher.Enqueue(() => {
					if(putCallback != null) {
						if (ex is ServiceException) {
							string errorCode = ((ServiceException)ex).ErrorCode;
							putCallback (errorCode);
							if(errorCode == "InvalidAccessKeyId") {
								FetchSTSToken();
							}
						} else {
							putCallback (ex.Message);
						}
					}
				});
			}
			finally
			{
//				_event.Set();
			}
		}

		public static void AsyncGetObject(string bucketName, string filename, string localFilename, Action<string> getCallback = null)
		{
			CreateDispatcherGameObject ();

			if (System.IO.File.Exists (localFilename)) {
				_dispatcher.Enqueue(() => {
					if(getCallback != null) {
						getCallback(localFilename);
					}
				});
				return;
			}

			try
			{
				var ossClient = new OssClient(endPoint, stsToken.AccessKeyId, stsToken.AccessKeySecret, stsToken.SecurityToken);

				Hashtable state = new Hashtable();
				state["client"] = ossClient;
				state["callback"] = getCallback;
				state["ossfilename"] = filename;
				state["localfilename"] = localFilename;

				ossClient.BeginGetObject(bucketName, filename, GetObjectCallback, state);

//				_event.WaitOne();
			}
			catch (OssException ex)
			{
				Console.WriteLine("Failed with error code: {0}; Error info: {1}. \nRequestID:{2}\tHostID:{3}",
					ex.ErrorCode, ex.Message, ex.RequestId, ex.HostId);
				if(getCallback != null) {
					if (ex is OssException) {
						getCallback (((OssException)ex).ErrorCode);
					} else {
						getCallback (ex.Message);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Failed with error info: {0}", ex.Message);
				if(getCallback != null) {
					if (ex is ServiceException) {
						getCallback (((ServiceException)ex).ErrorCode);
					} else {
						getCallback (ex.Message);
					}
				}
			}
		}

		private static void GetObjectCallback(IAsyncResult ar)
		{
			Hashtable state = (Hashtable)ar.AsyncState;
			Action<string> getCallback = (Action<string>)state["callback"];

			try
			{
				OssClient ossClient = (OssClient)state["client"];
				string ossfilename = (string)state["ossfilename"];
				string localfilename = (string)state["localfilename"];

				var result = ossClient.EndGetObject(ar);
				var metadata = result.Metadata;

				var requestStream = result.Content;
				var fs = new FileStream (localfilename, FileMode.OpenOrCreate);
				int bufLength = 4 * 1024;
				var buf = new byte[bufLength];

				MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
				md5.Initialize();

				int offset = 0;
				int contentLength = (int)metadata.ContentLength;
				while (offset < contentLength)
				{
					int readSize = bufLength;
					if (offset + readSize > contentLength)
					{
						readSize = contentLength - offset;
					}

					readSize = requestStream.Read(buf, 0, readSize);
					fs.Write(buf, 0, readSize);

					if (offset + readSize < contentLength) // 不是最后一块
					{
						md5.TransformBlock(buf, 0, Convert.ToInt32(readSize), buf, 0);
					}
					else // 最后一块
					{
						md5.TransformFinalBlock(buf, 0, Convert.ToInt32(readSize));
					}

					offset += readSize;
				}
				fs.Close();

				byte[] md5Hash = md5.Hash;
				md5.Clear();

				string ext = System.IO.Path.GetExtension(ossfilename);
				string buffermd5 = Utils.File.FormatMD5 (md5Hash) + ext;
				if(buffermd5 == ossfilename){
					_dispatcher.Enqueue(() => {
						if(getCallback != null) {
							getCallback(localfilename);
						}
					});
				} else {
					System.IO.File.Delete(localfilename);
					_dispatcher.Enqueue(() => {
						if(getCallback != null) {
							getCallback("CheckMD5Failed");
						}
					});
				}

				Console.WriteLine(ar.AsyncState as string);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				_dispatcher.Enqueue(() => {
					if(getCallback != null) {
						if (ex is ServiceException) {
							string errorCode = ((ServiceException)ex).ErrorCode;
							getCallback (errorCode);
							if(errorCode == "InvalidAccessKeyId") {
								FetchSTSToken();
							}
						} else {
							getCallback (ex.Message);
						}
					}
				});
			}
			finally
			{
//				_event.Set();
			}
		}

		private static void CreateDispatcherGameObject()
		{
			if (_dispatcher == null)
			{
				_dispatcher = new GameObject("OssClientDispatcher").AddComponent<Dispatcher>();
			}
		}
	}
}