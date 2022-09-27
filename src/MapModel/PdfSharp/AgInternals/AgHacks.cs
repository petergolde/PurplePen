using System;
using System.Collections.Generic;
using System.Net;
#if WPF || SILVERLIGHT
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
#endif

#if SILVERLIGHT
namespace PdfSharp
{
  public class ApplicationException : Exception
  {
    public ApplicationException()
    { }

    public ApplicationException(string message)
      : base(message)
    { }

    public ApplicationException(string message, Exception innerException)
      : base(message, innerException)
    { }
  }

  public class ArgumentOutOfRangeException : ArgumentException
  {
    public ArgumentOutOfRangeException()
    { }

    public ArgumentOutOfRangeException(string message)
      : base(message)
    { }

    public ArgumentOutOfRangeException(string message, string message2)
      : base(message, message2)
    { }

    public ArgumentOutOfRangeException(string message, object value, string message2)
      : base(message, message2)
    { }

    public ArgumentOutOfRangeException(string message, Exception innerException)
      : base(message, innerException)
    { }
  }

  public class InvalidEnumArgumentException : ArgumentException
  {
    public InvalidEnumArgumentException()
    { }

    public InvalidEnumArgumentException(string message)
      : base(message)
    { }

    public InvalidEnumArgumentException(string message, string message2)
      : base(message, message2)
    { }

    public InvalidEnumArgumentException(string message, int n, Type type)
      : base(message)
    { }

    public InvalidEnumArgumentException(string message, Exception innerException)
      : base(message, innerException)
    { }
  }

  //public class FileNotFoundException : Exception
  //{
  //  public FileNotFoundException()
  //  { }

  //  public FileNotFoundException(string message)
  //    : base(message)
  //  { }

  //  public FileNotFoundException(string message, string path)
  //    : base(message + "/" + path)
  //  { }

  //  public FileNotFoundException(string message, Exception innerException)
  //    : base(message, innerException)
  //  { }
  //}

  class Serializable : Attribute
  { }

  class BrowsableAttribute : Attribute
  {
    public BrowsableAttribute()
    { }

    public BrowsableAttribute(bool browsable)
    { }
  }

  public interface ICloneable
  {
    Object Clone();
  }
}
#endif
