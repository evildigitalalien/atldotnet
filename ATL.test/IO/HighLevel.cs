﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using ATL.AudioData;
using System.IO;
using System.Drawing;
using ATL.test.IO.MetaData;
using System.Collections.Generic;
using Commons;

namespace ATL.test.IO
{
    [TestClass]
    public class HighLevel
    {
        [TestMethod]
        public void TagIO_R_Single_ID3v1()
        {
            bool crossreadingDefault = MetaDataIOFactory.GetInstance().CrossReading;
            int[] tagPriorityDefault = new int[MetaDataIOFactory.TAG_TYPE_COUNT];
            MetaDataIOFactory.GetInstance().TagPriority.CopyTo(tagPriorityDefault, 0);

            /* Set options for Metadata reader behaviour - this only needs to be done once, or not at all if relying on default settings */
            MetaDataIOFactory.GetInstance().CrossReading = false;                            // default behaviour anyway
            MetaDataIOFactory.GetInstance().SetTagPriority(MetaDataIOFactory.TAG_APE, 0);    // No APEtag on sample file => should be ignored
            MetaDataIOFactory.GetInstance().SetTagPriority(MetaDataIOFactory.TAG_ID3V1, 1);  // Should be entirely read
            MetaDataIOFactory.GetInstance().SetTagPriority(MetaDataIOFactory.TAG_ID3V2, 2);  // Should not be read, since behaviour is single tag reading
            /* end set options */

            try
            {
                Track theTrack = new Track(TestUtils.GetResourceLocationRoot() + "MP3/01 - Title Screen.mp3");

                Assert.AreEqual("Nintendo Sound Scream", theTrack.Artist); // Specifically tagged like this on the ID3v1 tag
                Assert.AreEqual(0, theTrack.Year); // Specifically tagged as empty on the ID3v1 tag
            }
            finally
            {
                // Set back default settings
                MetaDataIOFactory.GetInstance().CrossReading = crossreadingDefault;
                MetaDataIOFactory.GetInstance().TagPriority = tagPriorityDefault;
            }
        }

        [TestMethod]
        public void TagIO_R_Multi()
        {
            bool crossreadingDefault = MetaDataIOFactory.GetInstance().CrossReading;
            int[] tagPriorityDefault = new int[MetaDataIOFactory.TAG_TYPE_COUNT];
            MetaDataIOFactory.GetInstance().TagPriority.CopyTo(tagPriorityDefault, 0);

            /* Set options for Metadata reader behaviour - this only needs to be done once, or not at all if relying on default settings */
            MetaDataIOFactory.GetInstance().CrossReading = true;
            MetaDataIOFactory.GetInstance().SetTagPriority(MetaDataIOFactory.TAG_APE, 0);    // No APEtag on sample file => should be ignored
            MetaDataIOFactory.GetInstance().SetTagPriority(MetaDataIOFactory.TAG_ID3V1, 1);  // Should be the main source except for the Year field (empty on ID3v1)
            MetaDataIOFactory.GetInstance().SetTagPriority(MetaDataIOFactory.TAG_ID3V2, 2);  // Should be used for the Year field (valuated on ID3v2)
            /* end set options */

            try
            {
                Track theTrack = new Track(TestUtils.GetResourceLocationRoot() + "MP3/01 - Title Screen.mp3");

                Assert.AreEqual("Nintendo Sound Scream", theTrack.Artist); // Specifically tagged like this on the ID3v1 tag
                Assert.AreEqual(1984, theTrack.Year); // Empty on the ID3v1 tag => cross-reading should read it on ID3v2
            }
            finally
            {
                // Set back default settings
                MetaDataIOFactory.GetInstance().CrossReading = crossreadingDefault;
                MetaDataIOFactory.GetInstance().TagPriority = tagPriorityDefault;
            }
        }

        [TestMethod]
        public void TagIO_R_MultiplePictures()
        {
            Track theTrack = new Track(TestUtils.GetResourceLocationRoot() + "OGG/bigPicture.ogg");

            // Check if _all_ embedded pictures are accessible from Track
            Assert.AreEqual(3, theTrack.EmbeddedPictures.Count);
        }

        [TestMethod]
        public void TagIO_RW_DeleteTag()
        {
            string testFileLocation = TestUtils.CopyAsTempTestFile("MP3/01 - Title Screen.mp3");
            Track theTrack = new Track(testFileLocation);

            theTrack.Remove(MetaDataIOFactory.TAG_ID3V2);

            Assert.AreEqual("Nintendo Sound Scream", theTrack.Artist); // Specifically tagged like this on the ID3v1 tag
            Assert.AreEqual(0, theTrack.Year); // Empty on the ID3v1 tag => should really come empty since ID3v2 tag has been removed

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        /// <summary>
        /// Check if the given file keeps its integrity after a no-op/neutral update
        /// </summary>
        /// <param name="resource"></param>
        private void tagIO_RW_UpdateNeutral(string resource)
        {
            string location = TestUtils.GetResourceLocationRoot() + resource;
            string testFileLocation = TestUtils.CopyAsTempTestFile(resource);
            Track theTrack = new Track(testFileLocation);
            theTrack.Save();

            theTrack = new Track(testFileLocation);

            // Check that the resulting file (working copy that has been processed) remains identical to the original file (i.e. no byte lost nor added)

            // 1- File length should be the same
            FileInfo originalFileInfo = new FileInfo(location);
            FileInfo testFileInfo = new FileInfo(testFileLocation);

            Assert.AreEqual(originalFileInfo.Length, testFileInfo.Length);

            // 2- File contents should be the same
            // NB : Due to field order differences, MD5 comparison is not possible yet

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_RW_UpdateNeutral()
        {
            tagIO_RW_UpdateNeutral("MP3/id3v2.4_UTF8.mp3"); // ID3v2
            tagIO_RW_UpdateNeutral("DSF/dsf.dsf"); // ID3v2 in DSF
            tagIO_RW_UpdateNeutral("FLAC/flac.flac"); // Vorbis-FLAC
            Settings.EnablePadding = true;
            try
            {
                tagIO_RW_UpdateNeutral("OGG/ogg.ogg"); // Vorbis-OGG
            }
            finally
            {
                Settings.EnablePadding = false;
            }
            tagIO_RW_UpdateNeutral("MP3/APE.mp3"); // APE
            // Native formats
            tagIO_RW_UpdateNeutral("VQF/vqf.vqf");
            tagIO_RW_UpdateNeutral("VGM/vgm.vgm");
            tagIO_RW_UpdateNeutral("SPC/spc.spc");
            tagIO_RW_UpdateNeutral("AAC/mp4.m4a");
            tagIO_RW_UpdateNeutral("WMA/wma.wma");
        }

        private void tagIO_RW_UpdateEmpty(string resource, bool supportsTrack = true)
        {
            string testFileLocation = TestUtils.CopyAsTempTestFile(resource);
            Track theTrack = new Track(testFileLocation);

            // Simple field
            theTrack.Artist = "Hey ho";
            // Tricky fields that aren't managed with a 1-to-1 mapping
            theTrack.Year = 1944;
            theTrack.TrackNumber = 10;
            theTrack.Save();

            theTrack = new Track(testFileLocation);

            Assert.AreEqual("Hey ho", theTrack.Artist);
            Assert.AreEqual(1944, theTrack.Year);
            if (supportsTrack) Assert.AreEqual(10, theTrack.TrackNumber);

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_RW_UpdateEmpty()
        {
//            Settings.DefaultTagsWhenNoMetadata = new int[2] { AudioData.MetaDataIOFactory.TAG_NATIVE, AudioData.MetaDataIOFactory.TAG_ID3V2 };
            try
            {
                tagIO_RW_UpdateEmpty("MP3/empty.mp3"); // ID3v2
                tagIO_RW_UpdateEmpty("DSF/empty.dsf"); // ID3v2 in DSF
                tagIO_RW_UpdateEmpty("FLAC/empty.flac"); // Vorbis-FLAC
                tagIO_RW_UpdateEmpty("OGG/empty.ogg"); // Vorbis-OGG
                tagIO_RW_UpdateEmpty("MP3/empty.mp3"); // APE
                // Native formats
                tagIO_RW_UpdateEmpty("VQF/empty.vqf");
                tagIO_RW_UpdateEmpty("VGM/empty.vgm", false);
                tagIO_RW_UpdateEmpty("SPC/empty.spc");

                tagIO_RW_UpdateEmpty("AAC/empty.m4a");
                tagIO_RW_UpdateEmpty("WMA/empty_full.wma");
            }
            finally
            {
                Settings.DefaultTagsWhenNoMetadata = new int[2] { AudioData.MetaDataIOFactory.TAG_ID3V2, AudioData.MetaDataIOFactory.TAG_NATIVE };
            }
        }

        private void tagIO_RW_UpdateTagBaseField(string resource, bool supportsDisc = true, bool supportsTotalTracksDiscs = true, bool supportsTrack = true)
        {
            string testFileLocation = TestUtils.CopyAsTempTestFile(resource);
            Track theTrack = new Track(testFileLocation);

            // Simple field
            theTrack.Artist = "Hey ho";
            // Tricky fields that aren't managed with a 1-to-1 mapping
            theTrack.Year = 1944;
            theTrack.TrackNumber = 10;
            theTrack.TrackTotal = 20;
            theTrack.DiscNumber = 30;
            theTrack.DiscTotal = 40;
            theTrack.Save();

            theTrack = new Track(testFileLocation);

            Assert.AreEqual("Hey ho", theTrack.Artist);
            Assert.AreEqual(1944, theTrack.Year);
            if (supportsTrack) Assert.AreEqual(10, theTrack.TrackNumber);
            if (supportsTotalTracksDiscs) Assert.AreEqual(20, theTrack.TrackTotal);
            if (supportsDisc) Assert.AreEqual(30, theTrack.DiscNumber);
            if (supportsTotalTracksDiscs) Assert.AreEqual(40, theTrack.DiscTotal);

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_RW_UpdateTagBaseField()
        {
            tagIO_RW_UpdateTagBaseField("MP3/id3v2.4_UTF8.mp3"); // ID3v2
            tagIO_RW_UpdateTagBaseField("DSF/dsf.dsf"); // ID3v2 in DSF
            tagIO_RW_UpdateTagBaseField("FLAC/flac.flac"); // Vorbis-FLAC
            tagIO_RW_UpdateTagBaseField("OGG/ogg.ogg"); // Vorbis-OGG
            tagIO_RW_UpdateTagBaseField("MP3/APE.mp3"); // APE
            // Specific formats
            tagIO_RW_UpdateTagBaseField("VQF/vqf.vqf", false, false);
            tagIO_RW_UpdateTagBaseField("VGM/vgm.vgm", false, false, false);
            tagIO_RW_UpdateTagBaseField("SPC/spc.spc", false, false);
            tagIO_RW_UpdateTagBaseField("AAC/mp4.m4a");
            tagIO_RW_UpdateTagBaseField("WMA/wma.wma");
        }

        [TestMethod]
        public void TagIO_RW_AddRemoveTagAdditionalField()
        {
            string testFileLocation = TestUtils.CopyAsTempTestFile("MP3/01 - Title Screen.mp3");
            Track theTrack = new Track(testFileLocation);

            theTrack.AdditionalFields.Add("ABCD", "efgh");
            theTrack.AdditionalFields.Remove("TENC");
            theTrack.Save();

            theTrack = new Track(testFileLocation);

            Assert.AreEqual(1, theTrack.AdditionalFields.Count); // TENC should have been removed
            Assert.IsTrue(theTrack.AdditionalFields.ContainsKey("ABCD"));
            Assert.AreEqual("efgh", theTrack.AdditionalFields["ABCD"]);

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_RW_UpdateTagAdditionalField()
        {
            string testFileLocation = TestUtils.CopyAsTempTestFile("MP3/01 - Title Screen.mp3");
            Track theTrack = new Track(testFileLocation);

            theTrack.AdditionalFields["TENC"] = "update test";
            theTrack.Save();

            theTrack = new Track(testFileLocation);

            Assert.AreEqual(1, theTrack.AdditionalFields.Count);
            Assert.IsTrue(theTrack.AdditionalFields.ContainsKey("TENC"));
            Assert.AreEqual("update test", theTrack.AdditionalFields["TENC"]);

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_RW_AddRemoveTagPictures()
        {
            string testFileLocation = TestUtils.CopyAsTempTestFile("MP3/id3v2.4_UTF8.mp3");
            Track theTrack = new Track(testFileLocation);

            theTrack.EmbeddedPictures.RemoveAt(1); // Remove Conductor; Front Cover remains

            // Add CD
            PictureInfo newPicture = new PictureInfo(Commons.ImageFormat.Gif, PictureInfo.PIC_TYPE.CD);
            newPicture.PictureData = File.ReadAllBytes(TestUtils.GetResourceLocationRoot() + "_Images/pic1.gif");
            theTrack.EmbeddedPictures.Add(newPicture);

            theTrack.Save();

            theTrack = new Track(testFileLocation);

            Assert.AreEqual(2, theTrack.EmbeddedPictures.Count); // Front Cover, CD

            bool foundFront = false;
            bool foundCD = false;

            foreach (PictureInfo pic in theTrack.EmbeddedPictures)
            {
                if (pic.PicType.Equals(PictureInfo.PIC_TYPE.Front)) foundFront = true;
                if (pic.PicType.Equals(PictureInfo.PIC_TYPE.CD)) foundCD = true;
            }

            Assert.IsTrue(foundFront);
            Assert.IsTrue(foundCD);

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_RW_UpdateTagPictures()
        {
            string testFileLocation = TestUtils.CopyAsTempTestFile("MP3/id3v2.4_UTF8.mp3");
            Track theTrack = new Track(testFileLocation);

            // Update Front picture
            PictureInfo newPicture = new PictureInfo(Commons.ImageFormat.Jpeg, PictureInfo.PIC_TYPE.Front);
            newPicture.PictureData = File.ReadAllBytes(TestUtils.GetResourceLocationRoot() + "_Images/pic2.jpg");
            theTrack.EmbeddedPictures.Add(newPicture);

            theTrack.Save();

            theTrack = new Track(testFileLocation);

            Assert.AreEqual(2, theTrack.EmbeddedPictures.Count); // Front Cover, Conductor

            bool foundFront = false;
            bool foundConductor = false;

            foreach (PictureInfo pic in theTrack.EmbeddedPictures)
            {
                if (pic.PicType.Equals(PictureInfo.PIC_TYPE.Front))
                {
                    foundFront = true;
                    Image picture = Image.FromStream(new MemoryStream(pic.PictureData));
                    Assert.AreEqual(picture.RawFormat, System.Drawing.Imaging.ImageFormat.Jpeg);
                    Assert.AreEqual(picture.Width, 900);
                    Assert.AreEqual(picture.Height, 290);
                }
                if (pic.PicType.Equals(PictureInfo.PIC_TYPE.Unsupported)) foundConductor = true;
            }

            Assert.IsTrue(foundFront);
            Assert.IsTrue(foundConductor);

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_RW_AddRemoveChapters()
        {
            string testFileLocation = TestUtils.CopyAsTempTestFile("MP3/chapters.mp3");
            Track theTrack = new Track(testFileLocation);

            theTrack.ChaptersTableDescription = "Content";
            theTrack.Chapters.RemoveAt(2);

            // Add new chapter
            ChapterInfo chapter = new ChapterInfo();
            chapter.StartTime = 440;
            chapter.StartOffset = 4400;
            chapter.EndTime = 880;
            chapter.EndOffset = 8800;
            chapter.UniqueID = "849849";
            chapter.Picture = new PictureInfo(ImageFormat.Jpeg, PictureInfo.PIC_TYPE.Generic);
            byte[] data = File.ReadAllBytes(TestUtils.GetResourceLocationRoot() + "_Images/pic1.jpeg");
            chapter.Picture.PictureData = data;
            theTrack.Chapters.Add(chapter);


            IList<ChapterInfo> chaptersSave = new List<ChapterInfo>(theTrack.Chapters);

            theTrack.Save();

            theTrack = new Track(testFileLocation);
            IList<PictureInfo> pics = theTrack.EmbeddedPictures; // Hack to load chapter pictures

            Assert.AreEqual("Content", theTrack.ChaptersTableDescription);
            Assert.AreEqual(chaptersSave.Count, theTrack.Chapters.Count);

            ChapterInfo readChapter;
            for (int i = 0; i < theTrack.Chapters.Count; i++)
            {
                readChapter = theTrack.Chapters[i];
                Assert.AreEqual(readChapter.StartOffset, chaptersSave[i].StartOffset);
                Assert.AreEqual(readChapter.StartTime, chaptersSave[i].StartTime);
                Assert.AreEqual(readChapter.EndOffset, chaptersSave[i].EndOffset);
                Assert.AreEqual(readChapter.EndTime, chaptersSave[i].EndTime);
                Assert.AreEqual(readChapter.Title, chaptersSave[i].Title);
                Assert.AreEqual(readChapter.Subtitle, chaptersSave[i].Subtitle);
                Assert.AreEqual(readChapter.UniqueID, chaptersSave[i].UniqueID);
                if (chaptersSave[i].Url != null)
                {
                    Assert.AreEqual(chaptersSave[i].Url.Description, readChapter.Url.Description);
                    Assert.AreEqual(chaptersSave[i].Url.Url, readChapter.Url.Url);
                }
                if (chaptersSave[i].Picture != null)
                {
                    Assert.IsNotNull(readChapter.Picture);
                    Assert.AreEqual(chaptersSave[i].Picture.ComputePicHash(), readChapter.Picture.ComputePicHash());
                }
            }

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_RW_UpdateTagChapters()
        {
            string testFileLocation = TestUtils.CopyAsTempTestFile("MP3/chapters.mp3");
            Track theTrack = new Track(testFileLocation);

            // Update 3rd chapter
            ChapterInfo chapter = new ChapterInfo(theTrack.Chapters[2]);
            chapter.Title = "updated title";
            chapter.Subtitle = "updated subtitle";
            chapter.Url = new ChapterInfo.UrlInfo("updated url");

            theTrack.Chapters[2] = chapter;

            theTrack.Save();

            theTrack = new Track(testFileLocation);

            Assert.AreEqual("toplevel toc", theTrack.ChaptersTableDescription);
            Assert.AreEqual(chapter.Title, theTrack.Chapters[2].Title);
            Assert.AreEqual(chapter.Subtitle, theTrack.Chapters[2].Subtitle);
            Assert.AreEqual(chapter.Url.Url, theTrack.Chapters[2].Url.Url);

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void TagIO_RW_UpdateKeepDataIntegrity()
        {
            Settings.EnablePadding = true;

            try
            {
                string resource = "OGG/ogg.ogg";
                string location = TestUtils.GetResourceLocationRoot() + resource;
                string testFileLocation = TestUtils.CopyAsTempTestFile(resource);
                Track theTrack = new Track(testFileLocation);

                string initialArtist = theTrack.Artist;
                theTrack.Artist = "Hey ho";
                theTrack.Save();

                theTrack = new Track(testFileLocation);

                theTrack.Artist = initialArtist;
                theTrack.Save();

                // Check that the resulting file (working copy that has been processed) remains identical to the original file (i.e. no byte lost nor added)
                FileInfo originalFileInfo = new FileInfo(location);
                FileInfo testFileInfo = new FileInfo(testFileLocation);

                Assert.AreEqual(originalFileInfo.Length, testFileInfo.Length);
                /* Not possible due to field order being changed
                                string originalMD5 = TestUtils.GetFileMD5Hash(location);
                                string testMD5 = TestUtils.GetFileMD5Hash(testFileLocation);

                                Assert.IsTrue(originalMD5.Equals(testMD5));
                */
                // Get rid of the working copy
                File.Delete(testFileLocation);
            }
            finally
            {
                Settings.EnablePadding = false;
            }
        }

        [TestMethod]
        public void StreamedIO_R_Audio()
        {
            string resource = "OGG/ogg.ogg";
            string location = TestUtils.GetResourceLocationRoot() + resource;
            string testFileLocation = TestUtils.CopyAsTempTestFile(resource);

            using (FileStream fs = new FileStream(testFileLocation, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Track theTrack = new Track(fs, "audio/ogg");

                Assert.AreEqual(33, theTrack.Duration);
                Assert.AreEqual(69, theTrack.Bitrate);
                Assert.AreEqual(22050, theTrack.SampleRate);
                Assert.AreEqual(true, theTrack.IsVBR);
                Assert.AreEqual(AudioDataIOFactory.CF_LOSSY, theTrack.CodecFamily);
            }

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }


        [TestMethod]
        public void StreamedIO_R_Meta()
        {
            string resource = "OGG/ogg.ogg";
            string location = TestUtils.GetResourceLocationRoot() + resource;
            string testFileLocation = TestUtils.CopyAsTempTestFile(resource);

            using (FileStream fs = new FileStream(testFileLocation, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Vorbis_OGG offTest = new Vorbis_OGG();
                offTest.TagIO_R_VorbisOGG_simple_OnePager(fs);
                fs.Seek(0, SeekOrigin.Begin); // Test if stream is still open
            }

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

        [TestMethod]
        public void StreamedIO_RW_Meta()
        {
            string resource = "OGG/empty.ogg";
            string location = TestUtils.GetResourceLocationRoot() + resource;
            string testFileLocation = TestUtils.CopyAsTempTestFile(resource);

            using (FileStream fs = new FileStream(testFileLocation, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                Vorbis_OGG offTest = new Vorbis_OGG();
                offTest.TagIO_RW_VorbisOGG_Empty(fs);
                fs.Seek(0, SeekOrigin.Begin); // Test if stream is still open
            }

            // Get rid of the working copy
            File.Delete(testFileLocation);
        }

    }
}
