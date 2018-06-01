using System;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;

public static class HttpExtensions
{
	public static Uri AddQuery(this Uri uri, string name, string value)
	{
		var ub = new UriBuilder(uri);
		if (value == null) {
			value = "";
		}
		String query = ub.Query;
		ub.Query = "";
		if (query != null && query != "") {
			query = query.Substring (1);
			query += "&";
		}
		ub.Query = query + Uri.EscapeDataString(name) + "=" + Uri.EscapeDataString(value);

		return ub.Uri;
	}
	public static Uri AddPath(this Uri uri, string newPath)
	{
		var ub = new UriBuilder(uri);
		ub.Path += "/" + newPath;
		return ub.Uri;
	}
}