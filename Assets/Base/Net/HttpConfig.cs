using System;
using UnityEngine;

public class HttpConfig {

//	static public string HOST = "http://127.0.0.1:8080/";
	static public string HOST = "http://api.xxxxxx.com/";
	static public string CDNHOST = "http://xxxx.xxxx.com/";

	static public Uri STS = new Uri (HOST + "stss");

	static public Uri CDN = new Uri (CDNHOST);
}
