using Microsoft.EntityFrameworkCore;
using System;

namespace LMPWebService.Models;
public class OuterMessageReader
{
    public int OuterMessageReader_ID { get; set; }
    public string OuterMessageReaderName { get; set; }
    public int? OuterSystem_ID { get; set; }
    public string OuterMessageSourceName { get; set; }
    public DateTime? LastSuccessReadDate { get; set; }
    public Guid? InsApplicationUser_ID { get; set; }
    public DateTime? InsDate { get; set; }
    public Guid? UpdApplicationUser_ID { get; set; }
    public DateTime? UpdDate { get; set; }
}