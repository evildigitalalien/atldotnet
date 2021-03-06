﻿using System.IO;
using System.Text;
using System.Collections.Generic;
using System;
using System.Xml;
using ATL.Logging;

namespace ATL.Playlist.IO
{
    /// <summary>
    /// ASX playlist manager
    /// 
    /// Implementation notes : Playlist items other than local files (e.g. file accessible via HTTP) are not supported
    /// </summary>
    public class ASXIO : PlaylistIO
    {
        protected override void getFiles(FileStream fs, IList<string> result)
        {
            using (XmlReader source = XmlReader.Create(fs))
            {
                while (source.ReadToFollowing("ENTRY"))
                {
                    while (source.Read())
                    {
                        if (source.NodeType == XmlNodeType.Element && source.Name.Equals("REF", StringComparison.OrdinalIgnoreCase)) parseLocation(source, result);
                        else if (source.NodeType == XmlNodeType.EndElement && source.Name.Equals("ENTRY", StringComparison.OrdinalIgnoreCase)) break;
                    }
                }
            }

        }

        private void parseLocation(XmlReader source, IList<string> result)
        {
            string href = source.GetAttribute("HREF");
            try
            {
                Uri uri = new Uri(href);
                if (uri.IsFile)
                {
                    if (!System.IO.Path.IsPathRooted(uri.LocalPath))
                    {
                        result.Add(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(FFileName), uri.LocalPath));
                    }
                    else
                    {
                        result.Add(uri.LocalPath);
                    }
                }
            }
            catch (UriFormatException)
            {
                LogDelegator.GetLogDelegate()(Log.LV_WARNING, result + " is not a valid URI");
            }
        }

        protected override void setTracks(FileStream fs, IList<Track> values)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.CloseOutput = false;
            settings.Encoding = Encoding.UTF8;

            XmlWriter writer = XmlWriter.Create(fs, settings);

            writer.WriteStartDocument();
            writer.WriteStartElement("ASX", "http://xspf.org/ns/0/");

            // Write the title.
            writer.WriteStartElement("TITLE");
            writer.WriteString("Playlist");
            writer.WriteEndElement();

            // Open tracklist
            foreach (Track t in values)
            {
                Uri trackUri = new Uri(t.Path, UriKind.RelativeOrAbsolute);

                writer.WriteStartElement("ENTRY");

                writer.WriteStartElement("REF");
                writer.WriteAttributeString("HREF", trackUri.IsAbsoluteUri ? trackUri.AbsolutePath : trackUri.OriginalString);
                writer.WriteEndElement();

                if (t.Title != null && t.Title.Length > 0)
                {
                    writer.WriteStartElement("TITLE");
                    writer.WriteString(t.Title);
                    writer.WriteEndElement();
                }

                if (t.Artist != null && t.Artist.Length > 0)
                {
                    writer.WriteStartElement("AUTHOR");
                    writer.WriteString(t.Artist);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement(); // entry
            }

            writer.WriteEndDocument();

            writer.Close();
        }
    }
}
