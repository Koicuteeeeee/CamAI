using System;
using System.Collections.Generic;

namespace CamAI.Service.AI.BLL.Models;

public class UserFaceRecord
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = Array.Empty<float>();
}

public class ApiFaceResponse
{
    public bool Success { get; set; }
    public List<UserFaceRecord> Data { get; set; } = new();
}
