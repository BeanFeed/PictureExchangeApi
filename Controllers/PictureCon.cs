using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Text;
using System.Linq;
//using System.Drawing;
//using System.Drawing.Imaging;

namespace PictureExchangeApi.Controllers;


[Route("api/psend")]
public class PictureSend : ControllerBase {
    private string EnvDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    [HttpPost("{userName}/{passwd}")]
    [Consumes("multipart/form-data")]
    public void psend(string userName, string passwd, [FromForm]IFormFile pho) {
        
        Console.WriteLine("New Image Post");
        
        //Console.WriteLine(res.imgb64);
        MemoryStream imgMs = new MemoryStream();
        pho.CopyTo(imgMs);
        byte[] imgBytes = imgMs.ToArray();
        //string imageBase64 = res.imgb64;
        //imageBase64 = imageBase64.Replace('-','/');
        if(!Directory.Exists(Path.Join(EnvDirectory,"imgs"))) Directory.CreateDirectory(Path.Join(EnvDirectory,"imgs"));
        Console.WriteLine(userName);
        if(!Directory.Exists(Path.Join(Path.Join(EnvDirectory,"imgs"), userName))) {Console.WriteLine("Fail 0"); return;}
        if(!System.IO.File.Exists(Path.Join(EnvDirectory,"imgs", userName,"pw") + ".txt")) {Console.WriteLine("Fail 1"); return;}
        if(System.IO.File.ReadAllText(Path.Join(EnvDirectory,"imgs", userName,"pw") + ".txt") != sha256(passwd)) {Console.WriteLine("Post Failed, Wrong Password"); return;}
        string curDate = JsonConvert.SerializeObject(new DateTime());
        //Writes a .txt file of the image encoded in Base64 
        FileStream file;
        if(!System.IO.File.Exists(Path.Join(EnvDirectory,"imgs", userName,"image") + ".txt")) {
            file = System.IO.File.Create(Path.Join(EnvDirectory,"imgs", userName,"image") + ".txt");
            file.Close();
        }
        //converts base64 to image, then compress, then back to base64
        
        var image = Image.Load(imgBytes);
        MemoryStream ms = new MemoryStream();
        image.Save(ms, new JpegEncoder {Quality = 60});
        string imageBase64 = Convert.ToBase64String(ms.ToArray());
        //writes the base64
        System.IO.File.WriteAllText(Path.Join(EnvDirectory,"imgs", userName,"image") + ".txt", imageBase64);
        //Writes a .json file of the date the image was received
        if(!System.IO.File.Exists(Path.Join(EnvDirectory,"imgs", userName,"dateRec") + ".json")) {
            file = System.IO.File.Create(Path.Join(EnvDirectory,"imgs", userName,"dateRec") + ".json");
            file.Close();
        }
        System.IO.File.WriteAllText(Path.Join(EnvDirectory,"imgs", userName,"dateRec") + ".json", JsonConvert.SerializeObject(DateOnly.FromDateTime(DateTime.Now)));
        
    }

    static string sha256(string randomString) {
    var crypt = new System.Security.Cryptography.SHA256Managed();
    var hash = new System.Text.StringBuilder();
    byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
    foreach (byte theByte in crypto) {
      hash.Append(theByte.ToString("x2"));
    }
    return hash.ToString();
    }

}

[Route("api/preceive")]
public class PictureReceive : ControllerBase {
    private string EnvDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    [HttpGet("{yourName}/{gettingName}/{pw}")]
    public string[] Get(string yourName, string gettingName, string pw) {
        Console.WriteLine("New Get");
        if(!Directory.Exists(Path.Join(EnvDirectory,"imgs"))) Directory.CreateDirectory(Path.Join(EnvDirectory,"imgs"));
        if(!Directory.Exists(Path.Join(Path.Join(EnvDirectory,"imgs"), yourName))) return new string[]{"-2",""};
        if(!Directory.Exists(Path.Join(Path.Join(EnvDirectory,"imgs"), gettingName))) return new string[]{"-4",""};
        if(!System.IO.File.Exists(Path.Join(EnvDirectory,"imgs",yourName,"pw.txt"))) return new string[]{"0","invalid1"};
        //Console.WriteLine(System.IO.File.ReadAllText(Path.Join(EnvDirectory,"imgs",yourID.ToString(),"pw.txt")));
        //Console.WriteLine(pw);
        if(System.IO.File.ReadAllText(Path.Join(EnvDirectory,"imgs",yourName,"pw.txt")) != sha256(pw)) return new string[]{"0","invalid2"}; 

        if(yourName != gettingName && !userCanAccess(gettingName, yourName)) return new string[]{"-4",""};
        //Code -2 = Self hasn't taken photo ever
        //Code -1 = Self hasn't taken photo today
        //Code -4 = OtherID hasn't taken photo ever
        //Code -3 = OtherID hasn't taken photo today
        //Code 0 = Can Exchange
        if(!System.IO.File.Exists(Path.Join(EnvDirectory,"imgs", yourName,"dateRec") + ".json")) return new string[]{"-2",""};
        string day = System.IO.File.ReadAllText(Path.Join(EnvDirectory,"imgs", yourName,"dateRec") + ".json");
        if(day != JsonConvert.SerializeObject(DateOnly.FromDateTime(DateTime.Now))) return new string[]{"-1",""};
        if(!System.IO.File.Exists(Path.Join(EnvDirectory,"imgs", gettingName,"dateRec") + ".json")) return new string[]{"-4",""};
        day = System.IO.File.ReadAllText(Path.Join(EnvDirectory,"imgs", gettingName,"dateRec") + ".json");
        if(day != JsonConvert.SerializeObject(DateOnly.FromDateTime(DateTime.Now))) return new string[]{"-3",""};

        return new string[]{"0",
        System.IO.File.ReadAllText(Path.Join(EnvDirectory,"imgs", gettingName,"image") + ".txt")
        //""
        };
        
    }
    static string sha256(string randomString) {
    var crypt = new System.Security.Cryptography.SHA256Managed();
    var hash = new System.Text.StringBuilder();
    byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
    foreach (byte theByte in crypto) {
      hash.Append(theByte.ToString("x2"));
    }
    return hash.ToString();
    }

    public bool userCanAccess(string userHoldingImage, string userAccessingImage) {
        if(!System.IO.File.Exists(Path.Join(EnvDirectory,"imgs", userHoldingImage,"shared.txt"))) System.IO.File.Create(Path.Join(EnvDirectory,"imgs", userHoldingImage,"shared.txt")).Close();
        var users = System.IO.File.ReadAllLines(Path.Join(EnvDirectory,"imgs", userHoldingImage,"shared.txt")).ToList<string>();
        if(users.Contains(userAccessingImage)) return true;
        return false;
    }
}

[Route("api/asetup")]
public class AccountSetup : ControllerBase {
  private string EnvDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
  [HttpPost("{wantedName}/{passwd}")]
  public string Post(string wantedName, string passwd) {
    Console.WriteLine("New Account Post");
    //Code 0: Success
    //Code -1: ID already taken
    if(!Directory.Exists(Path.Join(EnvDirectory,"imgs"))) Directory.CreateDirectory(Path.Join(EnvDirectory,"imgs"));
    //Console.WriteLine(Path.Join(EnvDirectory,"imgs",wantedName));
    //Console.WriteLine("Exists: " + Directory.Exists(Path.Join(EnvDirectory,"imgs",wantedName)).ToString());
    if(!Directory.Exists(Path.Join(EnvDirectory,"imgs",wantedName))) {
      Directory.CreateDirectory(Path.Join(EnvDirectory,"imgs",wantedName));
      FileStream file = System.IO.File.Create(Path.Join(EnvDirectory,"imgs",wantedName,"pw.txt"));
      file.Close();
      System.IO.File.WriteAllText(Path.Join(EnvDirectory,"imgs",wantedName,"pw.txt"), sha256(passwd));
      return "0";
    } else return "-1";
  }

  static string sha256(string randomString) {
    var crypt = new System.Security.Cryptography.SHA256Managed();
    var hash = new System.Text.StringBuilder();
    byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
    foreach (byte theByte in crypto) {
      hash.Append(theByte.ToString("x2"));
    }
    return hash.ToString();
    }
}

[Route("api/mngAccount")]
public class AccountManage : ControllerBase {
    private string EnvDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    [HttpPost("mngShare/{YourName}/{passwd}/{mode}/{OtherUser}")]
    public void Post(string YourName, string passwd, string mode, string OtherUser) { 
        if(!Directory.Exists(Path.Join(EnvDirectory,"imgs", YourName))) return;
        if(!System.IO.File.Exists(Path.Join(EnvDirectory,"imgs", YourName, "pw.txt"))) return;
        if(System.IO.File.ReadAllText(Path.Join(EnvDirectory,"imgs", YourName, "pw.txt")) != sha256(passwd)) return;
        if(!System.IO.File.Exists(Path.Join(EnvDirectory,"imgs", YourName,"shared.txt"))) System.IO.File.Create(Path.Join(EnvDirectory,"imgs", YourName,"shared.txt")).Close();

        if(mode == "add") {
            var users = System.IO.File.ReadAllLines(Path.Join(EnvDirectory,"imgs", YourName,"shared.txt")).ToList<string>();
            users.Add(OtherUser);
            System.IO.File.WriteAllLines(Path.Join(EnvDirectory,"imgs", YourName,"shared.txt"), users);
        }else if(mode == "remove") {
            var users = System.IO.File.ReadAllLines(Path.Join(EnvDirectory,"imgs", YourName,"shared.txt")).ToList<string>();
            if(users.Contains(OtherUser)) users.Remove(OtherUser);
            System.IO.File.WriteAllLines(Path.Join(EnvDirectory,"imgs", YourName,"shared.txt"), users);   
        }
    }

    [HttpGet("mngShare/{YourName}/{passwd}/getShared")]
    public string Get(string YourName, string passwd) {
        //Code -1: Failed
        if(!Directory.Exists(Path.Join(Path.Join(EnvDirectory,"imgs"), YourName))) {return new SharedPacket(-1, new string[0]).ToJSONString();}
        if(!System.IO.File.Exists(Path.Join(EnvDirectory,"imgs", YourName,"pw") + ".txt")) {return new SharedPacket(-2, new string[0]).ToJSONString();}
        //Console.WriteLine(sha256(passwd));
        if(System.IO.File.ReadAllText(Path.Join(EnvDirectory,"imgs", YourName,"pw") + ".txt") != sha256(passwd)) return new SharedPacket(-3, new string[0]).ToJSONString();
        if(!System.IO.File.Exists(Path.Join(EnvDirectory,"imgs", YourName,"shared") + ".txt")) {
            var file = System.IO.File.Create(Path.Join(EnvDirectory,"imgs", YourName,"shared") + ".txt");
            file.Close();
        }
        string[] users = System.IO.File.ReadAllLines(Path.Join(EnvDirectory,"imgs", YourName,"shared") + ".txt");
        return new SharedPacket(0, users).ToJSONString();

    }

    [HttpPost("changeLogin/{loginType}/{YourName}/{passwd}/{change}")]
    public void Post(int loginType, string YourName, string passwd, string change) {
        if(!Directory.Exists(Path.Join(EnvDirectory,"imgs", YourName))) return;
        if(!System.IO.File.Exists(Path.Join(EnvDirectory,"imgs", YourName, "pw.txt"))) return;
        if(System.IO.File.ReadAllText(Path.Join(EnvDirectory,"imgs", YourName, "pw.txt")) != sha256(passwd)) return;
        if(loginType == 0) System.IO.File.WriteAllText(Path.Join(EnvDirectory,"imgs",YourName,"pw.txt"), sha256(change));
        else if(loginType == 1) Directory.Move(Path.Join(EnvDirectory,"imgs", YourName), Path.Join(EnvDirectory,"imgs", change));

    }

    static string sha256(string randomString) {
    var crypt = new System.Security.Cryptography.SHA256Managed();
    var hash = new System.Text.StringBuilder();
    byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
    foreach (byte theByte in crypto) {
      hash.Append(theByte.ToString("x2"));
    }
    return hash.ToString();
    }

}

[Route("api/confirmAccount")]
public class AccountConfirm : ControllerBase {
    private string EnvDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
    [HttpGet("{name}/{passwd}")]
    public string Get(string name, string passwd) {
        Console.WriteLine("New Confirm Account Get");
        if(Directory.Exists(Path.Join(EnvDirectory,"imgs",name)) && System.IO.File.ReadAllText(Path.Join(EnvDirectory,"imgs",name,"pw.txt")) == sha256(passwd)) return "0";
        else return "-1";
    }

    static string sha256(string randomString) {
    var crypt = new System.Security.Cryptography.SHA256Managed();
    var hash = new System.Text.StringBuilder();
    byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(randomString));
    foreach (byte theByte in crypto) {
      hash.Append(theByte.ToString("x2"));
    }
    return hash.ToString();
  }
}

class SharedPacket {
    public int code {get; set;}
    public string[] users {get; set;}

    public SharedPacket(int code, string[] users) {
        this.code = code;
        this.users = users;
    }

    public string ToJSONString() {
        return JsonConvert.SerializeObject(this);
    }
}