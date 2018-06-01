using System;

[System.Serializable]
public class STSToken : BaseModel {
	public DateTime Expiration;
	public string AccessKeyId;
	public string AccessKeySecret;
	public string SecurityToken;
}
