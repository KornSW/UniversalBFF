using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore {

  //public class UserManagementBundleFileProvider : IFileProvider {

  //  private UserManagementHostingOptions _Opt;
  //  public UserManagementBundleFileProvider(UserManagementHostingOptions opt) {
  //    _Opt = opt;
  //  }

  //  private EmbeddedFileProvider _InnerProvider = new EmbeddedFileProvider(
  //    typeof(UserManagementBundleFileProvider).Assembly,
  //    "CtExpert.Module"
  //  );

  //  public IDirectoryContents GetDirectoryContents(string subpath) {
  //    if (subpath.StartsWith("/")) {
  //      subpath = subpath.Substring(1);
  //    }

  //    return _InnerProvider.GetDirectoryContents(subpath);

  //    //if (!subpath.StartsWith(_Opt.BaseUrl + "uimodule/")) {
  //    //  return new Microsoft.Extensions.FileProviders.NotFoundDirectoryContents();
  //    //}
  //    //return _InnerProvider.GetDirectoryContents("/");
  //  }

  //  public IFileInfo GetFileInfo(string subpath) {

  //    //if (!subpath.StartsWith(_Opt.BaseUrl + "uimodule/")) {
  //    //  return new Microsoft.Extensions.FileProviders.NotFoundFileInfo(subpath);
  //    //}
  //    if (subpath.StartsWith("/")) {
  //      subpath = subpath.Substring(1);
  //    }
  //    return new WrappedFileInfo(_InnerProvider.GetFileInfo(subpath), _Opt);
  //  }

  //  public IChangeToken Watch(string filter) {
  //    throw new System.NotImplementedException();
  //  }

  //  private class WrappedFileInfo : IFileInfo {

  //    private IFileInfo _InnerFileInfo;
  //    private UserManagementHostingOptions _Opt;

  //    public WrappedFileInfo(IFileInfo innerFileInfo, UserManagementHostingOptions opt) {
  //      _InnerFileInfo = innerFileInfo;
  //      _Opt = opt;
  //    }

  //    public bool Exists => _InnerFileInfo.Exists;
  //    public string PhysicalPath => _InnerFileInfo.PhysicalPath;
  //    public string Name => _InnerFileInfo.Name;
  //    public DateTimeOffset LastModified => _InnerFileInfo.LastModified;
  //    public bool IsDirectory => _InnerFileInfo.IsDirectory;

  //    public long Length {
  //      get {
  //        if (this.Name.EndsWith(".html")) {
  //          return this.GetModifiedContent().Length;
  //        }
  //        return _InnerFileInfo.Length;
  //      }
  //    }

  //    public Stream CreateReadStream() {
  //      if (this.Name.EndsWith(".html")) {
  //        var ms = new MemoryStream(Convert.ToInt32(this.Length));
  //        var sw = new StreamWriter(ms);
  //        sw.Write(this.GetModifiedContent());
  //        sw.Flush();
  //        ms.Position = 0;
  //        return ms;
  //      }
  //      return _InnerFileInfo.CreateReadStream();
  //    }

  //    private StringBuilder _ModifiedContent = null;
  //    private StringBuilder GetModifiedContent() {
  //      if (_ModifiedContent == null) {
  //        using (var stream = _InnerFileInfo.CreateReadStream()) {
  //          _ModifiedContent = new StringBuilder(Convert.ToInt32(stream.Length + 200));
  //          var sr = new StreamReader(stream);
  //          _ModifiedContent.Append(Regex.Replace(sr.ReadToEnd(), "(<title\\>[a-zA-Z0-9.-/ ()]*<\\/title\\>)", $"<title>CT-Expert</title>"));
  //          _ModifiedContent.Replace("<html>", "<html>\n  <!-- CTE.ModuleBundle -->");
  //          _ModifiedContent.Replace("<base href=\"/\"/>", $"<base href=\"{_Opt.BaseUrl}\"/>");
  //        }
  //      }
  //      return _ModifiedContent;
  //    }

  //  }

  //}

}
