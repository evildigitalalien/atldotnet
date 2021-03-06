﻿using ATL.Playlist;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ATL.test.IO.Playlist
{
    [TestClass]
    public class B4SIO
    {
        [TestMethod]
        public void PLIO_R_B4S()
        {
            string testFileLocation = TestUtils.CopyFileAndReplace(TestUtils.GetResourceLocationRoot() + "_Playlists/playlist.b4s", "$PATH", TestUtils.GetResourceLocationRoot(false));

            try
            {
                IPlaylistIO theReader = PlaylistIOFactory.GetInstance().GetPlaylistIO(testFileLocation);

                Assert.IsNotInstanceOfType(theReader, typeof(ATL.Playlist.IO.DummyIO));
                Assert.AreEqual(3, theReader.FilePaths.Count);
                foreach (string s in theReader.FilePaths) Assert.IsTrue(System.IO.File.Exists(s));
                foreach (Track t in theReader.Tracks) Assert.IsTrue(t.Duration > 0);
            }
            finally
            {
                File.Delete(testFileLocation);
            }
        }

        [TestMethod]
        public void PLIO_W_B4S()
        {
            IList<string> pathsToWrite = new List<string>();
            pathsToWrite.Add("aaa.mp3");
            pathsToWrite.Add("bbb.mp3");

            IList<Track> tracksToWrite = new List<Track>();
            tracksToWrite.Add(new Track(TestUtils.GetResourceLocationRoot() + "MP3/empty.mp3"));
            tracksToWrite.Add(new Track(TestUtils.GetResourceLocationRoot() + "MOD/mod.mod"));


            string testFileLocation = TestUtils.CreateTempTestFile("test.b4s");
            try
            {
                IPlaylistIO pls = PlaylistIOFactory.GetInstance().GetPlaylistIO(testFileLocation);

                // Test Path writing
                pls.FilePaths = pathsToWrite;
                IList<string> parents = new List<string>();
                int index = -1;

                using (FileStream fs = new FileStream(testFileLocation, FileMode.Open))
                using (XmlReader source = XmlReader.Create(fs))
                {
                    while (source.Read())
                    {
                        if (source.NodeType == XmlNodeType.Element)
                        {
                            if (source.Name.Equals("WinampXML", StringComparison.OrdinalIgnoreCase)) parents.Add(source.Name);
                            else if (source.Name.Equals("playlist", StringComparison.OrdinalIgnoreCase) && parents.Contains("WinampXML")) parents.Add(source.Name);
                            else if (source.Name.Equals("entry", StringComparison.OrdinalIgnoreCase) && parents.Contains("playlist"))
                            {
                                parents.Add(source.Name);
                                index++;
                                Assert.AreEqual("file:" + pathsToWrite[index], source.GetAttribute("Playstring"));
                            }
                        }
                    }
                }

                Assert.AreEqual(4, parents.Count);

                // Test Track writing
                pls.Tracks = tracksToWrite;
                index = -1;
                parents.Clear();

                using (FileStream fs = new FileStream(testFileLocation, FileMode.Open))
                using (XmlReader source = XmlReader.Create(fs))
                {
                    while (source.Read())
                    {
                        if (source.NodeType == XmlNodeType.Element)
                        {
                            if (source.Name.Equals("WinampXML", StringComparison.OrdinalIgnoreCase)) parents.Add(source.Name);
                            else if (source.Name.Equals("playlist", StringComparison.OrdinalIgnoreCase) && parents.Contains("WinampXML")) parents.Add(source.Name);
                            else if (source.Name.Equals("entry", StringComparison.OrdinalIgnoreCase) && parents.Contains("playlist"))
                            {
                                parents.Add(source.Name);
                                index++;
                                Assert.AreEqual("file:" + tracksToWrite[index].Path, source.GetAttribute("Playstring"));
                            }
                            else if (parents.Contains("entry"))
                            {
                                if (source.Name.Equals("Name", StringComparison.OrdinalIgnoreCase)) Assert.AreEqual(tracksToWrite[index].Title, getXmlValue(source));
                                else if (source.Name.Equals("Length", StringComparison.OrdinalIgnoreCase)) Assert.AreEqual(((long)Math.Round(tracksToWrite[index].DurationMs)).ToString(), getXmlValue(source));
                            }
                        }
                    }
                }
                Assert.AreEqual(4, parents.Count);
            }
            finally
            {
                File.Delete(testFileLocation);
            }
        }

        private static string getXmlValue(XmlReader source)
        {
            source.Read();
            if (source.NodeType == XmlNodeType.Text)
            {
                return source.Value;
            }
            return "";
        }
    }
}
