using System;
using System.Net;
using CI.HttpClient;


namespace GameNet
{
	public class HttpMethod {
		static Session session = new Session ();

		static public Session Session{
			get {
				return session;
			}
		}

		static public void Post(Uri uri, bool auth, IHttpContent content, Action<HttpResponseMessage<string>> responseCallback, Action<UploadStatusMessage> uploadStatusCallback = null)
		{
			HttpClient client = new HttpClient ();
			if (auth && session.token != null) {
				client.Headers.Add (HttpRequestHeader.Authorization, "Bearer " + session.token);
			}
			if (content == null) {
				content = new StringContent("");
			}
			client.Post (uri, content, responseCallback, uploadStatusCallback);
		}

		static public void GetString(Uri uri, bool auth, Action<HttpResponseMessage<string>> responseCallback)
		{
			HttpClient client = new HttpClient ();
			if (auth && session.token != null) {
				client.Headers.Add (HttpRequestHeader.Authorization, "Bearer " + session.token);
			}
			client.GetString (uri, responseCallback);
		}

		static public void Download(Uri uri, Action<HttpResponseMessage<byte[]>> responseCallback)
		{
			HttpClient client = new HttpClient();
			client.GetByteArray (uri, HttpCompletionOption.StreamResponseContent, responseCallback);
		}

		static public void Put(Uri uri, bool auth, IHttpContent content, Action<HttpResponseMessage<string>> responseCallback, Action<UploadStatusMessage> uploadStatusCallback = null)
		{
			HttpClient client = new HttpClient ();
			if (auth && session.token != null) {
				client.Headers.Add (HttpRequestHeader.Authorization, "Bearer " + session.token);
			}
			if (content == null) {
				content = new StringContent("");
			}
			client.Put (uri, content, responseCallback, uploadStatusCallback);
		}

		static public void Delete(Uri uri, bool auth, Action<HttpResponseMessage<string>> responseCallback)
		{
			HttpClient client = new HttpClient ();
			if (auth && session.token != null) {
				client.Headers.Add (HttpRequestHeader.Authorization, "Bearer " + session.token);
			}
			client.Delete (uri, responseCallback);
		}

		static public void SaveSession()
		{
			Session.Save ("Session.dat");
		}

		static public bool LoadSession()
		{
			session.deviceId = System.Guid.NewGuid ().ToString ();
			return Session.Load<Session> ("Session.dat", ref session);
		}

		static public void RemoveSession()
		{
			Session.Remove<Session> ("Session.dat");
		}
	}
}
