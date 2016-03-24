// WinZipAesTests.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa and Microsoft Corporation.
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License.
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// last saved (in emacs):
// Time-stamp: <2010-February-10 11:40:28>
//
// ------------------------------------------------------------------
//
// This module defines the tests of the WinZIP AES Encryption capability
// of DotNetZip.
//
// ------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Ionic.Zip;
using Ionic.Zip.Tests.Utilities;

namespace Ionic.Zip.Tests.WinZipAes
{
    /// <summary>
    /// Summary description for WinZipAesTests
    /// </summary>
    [TestClass]
    public class WinZipAesTests : IonicTestClass
    {
        public WinZipAesTests() : base() { }


        [TestMethod]
        public void WinZipAes_CreateZip()
        {
            _Internal_CreateZip_WinZipAes("WinZipAes_CreateZip", 14400, 5000);
        }

        [TestMethod]
        public void WinZipAes_CreateZip_VerySmallFiles()
        {
            _Internal_CreateZip_WinZipAes("WinZipAes_CreateZip_VerySmallFiles", 14, 5);
        }



        private void _Internal_CreateZip_WinZipAes(string name, int size1, int size2)
        {
            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [_Internal_CreateZip_WinZipAes] : winzip is not present");
                return;
            }

            string filename = null;
            int entries = _rnd.Next(11) + 8;
            var checksums = new Dictionary<string, string>();
            var filesToZip = new List<string>();
            for (int i = 0; i < entries; i++)
            {
                int filesize = _rnd.Next(size1) + size2;
                if (_rnd.Next(2) == 1)
                {
                    filename = Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                    TestUtilities.CreateAndFillFileBinary(filename, filesize);
                }
                else
                {
                    filename = Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                    TestUtilities.CreateAndFillFileText(filename, filesize);
                }

                var chk = TestUtilities.ComputeChecksum(filename);
                checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                filesToZip.Add(filename);
            }


            Ionic.Zip.EncryptionAlgorithm[] EncOptions = {
                EncryptionAlgorithm.None,
                EncryptionAlgorithm.WinZipAes256,
                EncryptionAlgorithm.WinZipAes128,
                EncryptionAlgorithm.PkzipWeak
            };

            for (int k = 0; k < EncOptions.Length; k++)
            {

                for (int m = 0; m < 2; m++)
                {
                    //string password = TestUtilities.GenerateRandomPassword();
                    string password = Path.GetRandomFileName().Replace(".", "-");
                    Directory.SetCurrentDirectory(TopLevelDir);
                    TestContext.WriteLine("\n\n==================Trial {0}.{1}..", k, m);
                    string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("{0}-{1}-{2}.zip", name, k, m));

                    TestContext.WriteLine("Creating file {0}", zipFileToCreate);
                    TestContext.WriteLine("  Password:   {0}", password);
                    TestContext.WriteLine("  Encryption:       {0}", EncOptions[k].ToString());
                    TestContext.WriteLine("  NonSeekable output:   {0}", (m == 0) ? "No" : "Yes");
                    TestContext.WriteLine("  #entries:       {0}", entries);

                    string comment = String.Format("This archive uses Encryption: {0}, password({1}), NonSeekable=({2})",
                                                   EncOptions[k], password, (m == 0) ? "No" : "Yes");

                    _DotNetZip_CreateZip(filesToZip, EncOptions[k], password, comment, zipFileToCreate, (m==1));

                    if (EncOptions[k] == EncryptionAlgorithm.None)
                        BasicVerifyZip(zipFileToCreate);
                    else
                        BasicVerifyZip(zipFileToCreate, password);


                    TestContext.WriteLine("---------------Reading {0}...", zipFileToCreate);
                    System.Threading.Thread.Sleep(1200); // seems to be a race condition?  sometimes?
                    using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                    {
                        string extractDir = String.Format("extract-{0}.{1}", k, m);
                        foreach (var e in zip2)
                        {
                            TestContext.WriteLine(" Entry: {0}  c({1})  unc({2})", e.FileName, e.CompressedSize, e.UncompressedSize);
                            Assert.AreEqual<EncryptionAlgorithm>(EncOptions[k], e.Encryption);
                            e.ExtractWithPassword(extractDir, password);
                            filename = Path.Combine(extractDir, e.FileName);
                            string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                            Assert.IsTrue(checksums.ContainsKey(e.FileName), "Checksum is missing");
                            Assert.AreEqual<string>(checksums[e.FileName], actualCheckString, "Checksums for ({0}) do not match.", e.FileName);
                            TestContext.WriteLine("     Checksums match ({0}).\n", actualCheckString);
                        }
                    }
                }
            }
        }

        private static void _DotNetZip_CreateZip(List<string> filesToZip,
                                                 EncryptionAlgorithm encryption,
                                                 string password,
                                                 string comment,
                                                 string zipFileToCreate,
                                                 bool nonSeekable)
        {
            // Want to test the library when saving to non-seekable output streams.  Like
            // stdout or ASPNET's Response.OutputStream.  This simulates it.
            if (nonSeekable)
            {
                using (var outStream = new Ionic.Zip.Tests.NonSeekableOutputStream(File.Create(zipFileToCreate)))
                {
                    using (ZipFile zip1 = new ZipFile())
                    {
                        zip1.Encryption = encryption;
                        if (zip1.Encryption != EncryptionAlgorithm.None)
                            zip1.Password = password;

                        zip1.AddFiles(filesToZip, "");
                        zip1.Comment = comment;
                        zip1.Save(outStream);
                    }
                }
            }
            else
            {
                using (ZipFile zip1 = new ZipFile())
                {
                    zip1.Encryption = encryption;
                    if (zip1.Encryption != EncryptionAlgorithm.None)
                        zip1.Password = password;

                    zip1.AddFiles(filesToZip, "");
                    zip1.Comment = comment;
                    zip1.Save(zipFileToCreate);
                }
            }
        }




        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.BadPasswordException))]
        public void WinZipAes_CreateZip_NoPassword()
        {
            Directory.SetCurrentDirectory(TopLevelDir);
            string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("WinZipAes_CreateZip_NoPassword.zip"));

            TestContext.WriteLine("Creating file {0}", zipFileToCreate);
            int entries = _rnd.Next(11) + 8;

            string filename = null;
            var checksums = new Dictionary<string, string>();
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Encryption = EncryptionAlgorithm.WinZipAes256;
                for (int i = 0; i < entries; i++)
                {
                    if (_rnd.Next(2) == 1)
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileBinary(filename, filesize);
                    }
                    else
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileText(filename, filesize);
                    }
                    zip1.AddFile(filename, "");

                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                }

                zip1.Comment = String.Format("This archive uses Encryption: {0}, no password!", zip1.Encryption);
                // With no password, we expect no encryption in the output.
                zip1.Save(zipFileToCreate);
            }

            #if NOT
            WinzipVerify(zipFileToCreate);

            // validate all the checksums
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    if (!e.IsDirectory)
                    {
                        e.Extract("unpack");
                        string PathToExtractedFile = Path.Combine("unpack", e.FileName);

                        Assert.IsTrue(e.Encryption == EncryptionAlgorithm.None);
                        Assert.IsTrue(checksums.ContainsKey(e.FileName));

                        // verify the checksum of the file is correct
                        string expectedCheckString = checksums[e.FileName];
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                        Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                    }
                }
            }
            #endif

        }



        [TestMethod]
        public void WinZipAes_CreateZip_DirectoriesOnly()
        {
            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [WinZipAes_CreateZip_DirectoriesOnly] : winzip is not present");
                return;
            }

            string zipFileToCreate = Path.Combine(TopLevelDir, "WinZipAes_CreateZip_DirectoriesOnly.zip");
            Assert.IsFalse(File.Exists(zipFileToCreate), "The temporary zip file '{0}' already exists.", zipFileToCreate);

            string password = TestUtilities.GenerateRandomPassword();
            string DirToZip = Path.Combine(TopLevelDir, "zipthis");
            Directory.CreateDirectory(DirToZip);

            int entries = 0;
            int subdirCount = _rnd.Next(8) + 8;

            TestContext.WriteLine("Creating file   {0}", zipFileToCreate);
            TestContext.WriteLine("  Password:     {0}", password);
            TestContext.WriteLine("  #directories: {0}", subdirCount);

            for (int i = 0; i < subdirCount; i++)
            {
                string SubDir = Path.Combine(DirToZip, "EmptyDir" + i);
                Directory.CreateDirectory(SubDir);
            }

            Directory.SetCurrentDirectory(TopLevelDir);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip1.Password = password;
                zip1.AddDirectory(Path.GetFileName(DirToZip));
                zip1.Save(zipFileToCreate);
            }

            BasicVerifyZip(zipFileToCreate, password);

            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), entries,
                                 "The zip file created has the wrong number of entries.");
        }



        [TestMethod]
        public void WinZipAes_CreateZip_ZeroLengthFiles_256()
        {
            string password = TestUtilities.GenerateRandomPassword(12);
            _Internal_CreateZip_ZeroLengthFiles(password, EncryptionAlgorithm.WinZipAes256);
        }
        [TestMethod]
        public void WinZipAes_CreateZip_ZeroLengthFiles_128()
        {
            string password = TestUtilities.GenerateRandomPassword(12);
            _Internal_CreateZip_ZeroLengthFiles(password, EncryptionAlgorithm.WinZipAes128);
        }

        [TestMethod]
        public void WinZipAes_CreateZip_ZeroLengthFiles_NoPassword_256()
        {
            _Internal_CreateZip_ZeroLengthFiles(null, EncryptionAlgorithm.WinZipAes256);
        }
        [TestMethod]
        public void WinZipAes_CreateZip_ZeroLengthFiles_NoPassword_128()
        {
            _Internal_CreateZip_ZeroLengthFiles(null, EncryptionAlgorithm.WinZipAes128);
        }

        public void _Internal_CreateZip_ZeroLengthFiles(string password, EncryptionAlgorithm algorithm)
        {
            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [_Internal_CreateZip_ZeroLengthFiles] : winzip is not present");
                return;
            }

            string zipFileToCreate = Path.Combine(TopLevelDir, "WinZipAes_CreateZip_ZeroLengthFiles.zip");
            Assert.IsFalse(File.Exists(zipFileToCreate), "The temporary zip file '{0}' already exists.", zipFileToCreate);

            TestContext.WriteLine("Creating file {0}", zipFileToCreate);
            TestContext.WriteLine("  Password:   {0}", password);

            // create a bunch of zero-length files
            int entries = _rnd.Next(21) + 5;
            int i;
            string[] filesToZip = new string[entries];
            for (i = 0; i < entries; i++)
                filesToZip[i] = TestUtilities.CreateUniqueFile("zerolength", TopLevelDir);

            using (ZipFile zip = new ZipFile())
            {
                zip.Encryption = algorithm;
                zip.Password = password;
                zip.AddFiles(filesToZip);
                zip.Save(zipFileToCreate);
            }

            BasicVerifyZip(zipFileToCreate, password);

            Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), filesToZip.Length,
                                 "The zip file created has the wrong number of entries.");
        }



        private void WinzipCreate(string zipfile, string fileOrDir, string encryptionArg, string password)
        {
            string[] files = { fileOrDir };
            WinzipCreate(zipfile, files, encryptionArg, password);
        }

        private void WinzipCreate(string zipfile, IEnumerable<string> files, string encryptionArg, string password)
        {
            string args = null;
            if (password == null)
            {
                args = String.Format("-a -whs {0}", zipfile);
            }
            else
            {
                args = String.Format("-a -whs -s\"{0}\"  {1}  {2}", password, encryptionArg, zipfile);
            }

            // this better not be too long a list, otherwise the cmd line length limit will be exceeded.
            foreach (var f in files)
                args += " " + f;

            string wzzipOut = this.Exec(wzzip, args);
        }




        [TestMethod]
        public void WinZipAes_ReadEncryptedZips()
        {
            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [WinZipAes_ReadEncryptedZips] : winzip is not present");
                return;
            }

            _Internal_ReadEncryptedZips(true);
        }


        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.BadPasswordException))]
        public void WinZipAes_ReadZip_Fail_BadPassword()
        {
            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [WinZipAes_ReadZip_Fail_BadPassword] : winzip is not present");
                throw new Ionic.Zip.BadPasswordException();
            }

            _Internal_ReadEncryptedZips(false);
        }


        private void _Internal_ReadEncryptedZips(bool correctPw)
        {
            string[] cryptoArg = new string[] { "-ycAES128", "-ycAES256", };
            for (int m = 0; m < cryptoArg.Length; m++)
            {
                Directory.SetCurrentDirectory(TopLevelDir);

                // get a set of files to zip up
                string subdir = Path.Combine(TopLevelDir, "files" + m);
                string[] filesToZip;
                Dictionary<string, byte[]> checksums;
                Compatibility.CreateFilesAndChecksums(subdir, out filesToZip, out checksums);
                string password = TestUtilities.GenerateRandomPassword();
                string[] dirsToZip = new string[]
                    {
                        subdir + "\\*.*",
                    };
                string zipFileToCreate = String.Format("WinZipAes_ReadZips-{0}.zip", m);
                WinzipCreate(zipFileToCreate, dirsToZip, cryptoArg[m], password);

                _Internal_ReadZip(zipFileToCreate, (correctPw) ? password : null, filesToZip.Length);
            }
        }


        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.BadPasswordException))]
        public void WinZipAes_ReadZip_Fail_NoPassword_128()
        {
            string password = TestUtilities.GenerateRandomPassword();
            _Internal_GenerateFiles_CreateZip("-ycAES128", password, 1);
        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.BadPasswordException))]
        public void WinZipAes_ReadZip_Fail_NoPassword_256()
        {
            string password = TestUtilities.GenerateRandomPassword();
            _Internal_GenerateFiles_CreateZip("-ycAES256", password, 1);
        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.BadPasswordException))]
        public void WinZipAes_ReadZip_Fail_WrongPassword()
        {
            string password = TestUtilities.GenerateRandomPassword();
            _Internal_GenerateFiles_CreateZip("-ycAES256", password, 2);
        }

        [TestMethod]
        [ExpectedException(typeof(Ionic.Zip.BadPasswordException))]
        public void WinZipAes_ReadZip_Fail_WrongMethod()
        {
            string password = TestUtilities.GenerateRandomPassword();
            _Internal_GenerateFiles_CreateZip("-ycAES256", password, 3);
        }


        public void _Internal_GenerateFiles_CreateZip(string cryptoArg, string password, int pwFlavor)
        {
            // This method generates files, then zips them up using WinZip, then tries
            // to unzip using DotNetZip. It is parameterized to test both success and
            // failure cases. If pwFlavor is zero, then it uses the correct password to
            // unzip, and everything should work.  If pwFlavor is non-zero then it uses
            // some incorrect password, either null or a bogus string, and generates a failure.

            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [_Internal_GenerateFiles_CreateZip] : winzip is not present");

                // either return or throw, depending on the expected results.
                if (pwFlavor == 0)
                    return;
                throw new Ionic.Zip.BadPasswordException();
            }

            Directory.SetCurrentDirectory(TopLevelDir);
            // get a set of files to zip up
            string subdir = Path.Combine(TopLevelDir, "files");

            string[] filesToZip = TestUtilities.GenerateFilesFlat(subdir);

            string zipFileToCreate = "_Internal_GenerateFiles_CreateZip.zip";

            WinzipCreate(zipFileToCreate, subdir, cryptoArg, password);

            string pwForReading = (pwFlavor == 0)
                ? password
                : (pwFlavor == 1)
                ? null
                : (pwFlavor == 2)
                ? "-wrongpassword-"
                : TestUtilities.GenerateRandomPassword();

            _Internal_ReadZip(zipFileToCreate, pwForReading, filesToZip.Length);
        }



        int zipCount = 0;
        private void _Internal_ReadZip(string zipFileToRead, string password, int expectedFilesExtracted)
        {
            Directory.SetCurrentDirectory(TopLevelDir);

            Assert.IsTrue(File.Exists(zipFileToRead), "The zip file '{0}' does not exist.", zipFileToRead);

            // extract all the files
            int actualFilesExtracted = 0;
            string extractDir = String.Format("Extract{0}", zipCount++);

            using (ZipFile zip2 = ZipFile.Read(zipFileToRead))
            {
                //zip2.Password = password;
                foreach (ZipEntry e in zip2)
                {
                    if (!e.IsDirectory)
                    {
                        if (password == "-null-")
                            e.Extract(extractDir);
                        else
                            e.ExtractWithPassword(extractDir, password);
                        actualFilesExtracted++;
                    }
                }
            }
            Assert.AreEqual<int>(expectedFilesExtracted, actualFilesExtracted);
        }




        [TestMethod]
        public void WinZipAes_CreateZip_NoCompression()
        {
            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [WinZipAes_CreateZip_NoCompression] : winzip is not present");
                return;
            }

            Directory.SetCurrentDirectory(TopLevelDir);
            string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("WinZipAes_CreateZip_NoCompression.zip"));
            string password = TestUtilities.GenerateRandomPassword();

            TestContext.WriteLine("=======================================");
            TestContext.WriteLine("Creating file {0}", zipFileToCreate);
            TestContext.WriteLine("  Password:   {0}", password);
            int entries = _rnd.Next(21) + 5;

            string filename = null;
            var checksums = new Dictionary<string, string>();
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip1.Password = password;
                zip1.CompressionLevel = Ionic.Zlib.CompressionLevel.None;

                for (int i = 0; i < entries; i++)
                {
                    if (_rnd.Next(2) == 1)
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileBinary(filename, filesize);
                    }
                    else
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileText(filename, filesize);
                    }
                    zip1.AddFile(filename, "");

                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                }

                zip1.Comment = String.Format("This archive uses Encryption({0}) password({1}) no compression.", zip1.Encryption, password);
                zip1.Save(zipFileToCreate);
            }

            BasicVerifyZip(zipFileToCreate, password);

            // validate all the checksums
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    if (!e.IsDirectory)
                    {
                        Assert.AreEqual<short>(0, (short)e.CompressionMethod);

                        e.ExtractWithPassword("unpack", password);

                        string PathToExtractedFile = Path.Combine("unpack", e.FileName);

                        Assert.IsTrue(checksums.ContainsKey(e.FileName));

                        // verify the checksum of the file is correct
                        string expectedCheckString = checksums[e.FileName];
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                        Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                    }
                }
            }
        }



        [TestMethod]
        public void WinZipAes_CreateZip_EmptyPassword()
        {
            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [WinZipAes_CreateZip_EmptyPassword] : winzip is not present");
                return;
            }

            Directory.SetCurrentDirectory(TopLevelDir);
            string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("WinZipAes_CreateZip_EmptyPassword.zip"));
            string password = "";

            TestContext.WriteLine("=======================================");
            TestContext.WriteLine("Creating file {0}", zipFileToCreate);
            TestContext.WriteLine("  Password:   '{0}'", password);
            int entries = _rnd.Next(21) + 5;

            string filename = null;
            var checksums = new Dictionary<string, string>();
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip1.Password = password;

                for (int i = 0; i < entries; i++)
                {
                    if (_rnd.Next(2) == 1)
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileBinary(filename, filesize);
                    }
                    else
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileText(filename, filesize);
                    }
                    zip1.AddFile(filename, "");

                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                }

                zip1.Comment = String.Format("This archive uses Encryption({0}) password({1}) no compression.", zip1.Encryption, password);
                zip1.Save(zipFileToCreate);
            }

            BasicVerifyZip(zipFileToCreate);

            // validate all the checksums
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    if (!e.IsDirectory)
                    {
                        e.ExtractWithPassword("unpack", password);

                        string PathToExtractedFile = Path.Combine("unpack", e.FileName);

                        Assert.IsTrue(checksums.ContainsKey(e.FileName));

                        // verify the checksum of the file is correct
                        string expectedCheckString = checksums[e.FileName];
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(PathToExtractedFile));
                        Assert.AreEqual<String>(expectedCheckString, actualCheckString, "Unexpected checksum on extracted filesystem file ({0}).", PathToExtractedFile);
                    }
                }
            }
        }


        [TestMethod]
        public void WinZipAes_RemoveEntryAndSave()
        {
            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [WinZipAes_RemoveEntryAndSave] : winzip is not present");
                return;
            }

            // make a few text files
            string[] TextFiles = new string[5];
            for (int i = 0; i < TextFiles.Length; i++)
            {
                TextFiles[i] = Path.Combine(TopLevelDir, String.Format("TextFile{0}.txt", i));
                TestUtilities.CreateAndFillFileText(TextFiles[i], _rnd.Next(4000) + 5000);
            }
            TestContext.WriteLine(new String('=', 66));
            TestContext.WriteLine("RemoveEntryAndSave()");
            string password = Path.GetRandomFileName();
            for (int k = 0; k < 2; k++)
            {
                TestContext.WriteLine(new String('-', 55));
                TestContext.WriteLine("Trial {0}", k);
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("RemoveEntryAndSave-{0}.zip", k));

                // create the zip: add some files, and Save() it
                using (ZipFile zip = new ZipFile())
                {
                    if (k == 1)
                    {
                        TestContext.WriteLine("Specifying a password...");
                        zip.Password = password;
                        zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                    }
                    for (int i = 0; i < TextFiles.Length; i++)
                        zip.AddFile(TextFiles[i], "");

                    zip.AddEntry("Readme.txt", "This is the content of the file. Ho ho ho!");
                    TestContext.WriteLine("Save...");
                    zip.Save(zipFileToCreate);
                }

                if (k == 1)
                    BasicVerifyZip(zipFileToCreate, password);

                // remove a file and re-Save
                using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                {
                    int entryToRemove = _rnd.Next(TextFiles.Length);
                    TestContext.WriteLine("Removing an entry...: {0}", Path.GetFileName(TextFiles[entryToRemove]));
                    zip2.RemoveEntry(Path.GetFileName(TextFiles[entryToRemove]));
                    zip2.Save();
                }

                // Verify the files are in the zip
                Assert.AreEqual<int>(TestUtilities.CountEntries(zipFileToCreate), TextFiles.Length,
                                     String.Format("Trial {0}: The Zip file has the wrong number of entries.", k));

                if (k == 1)
                    BasicVerifyZip(zipFileToCreate, password);
            }
        }


        [TestMethod]
        public void WinZipAes_SmallBuffers_wi7967()
        {
            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [WinZipAes_SmallBuffers_wi7967] : winzip is not present");
                return;
            }

            Directory.SetCurrentDirectory(TopLevelDir);

            string password = Path.GetRandomFileName() + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

            int[] sizes = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 13, 21, 35, 93 };
            for (int i = 0; i < sizes.Length; i++)
            {
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("WinZipAes_SmallBuffers_wi7967-{0}.zip", i));
                //MemoryStream zippedStream = new MemoryStream();
                byte[] buffer = new byte[sizes[i]];
                _rnd.NextBytes(buffer);
                MemoryStream source = new MemoryStream(buffer);

                using (var zip = new ZipFile())
                {
                    source.Seek(0, SeekOrigin.Begin);
                    zip.Password = password;
                    zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                    zip.AddEntry(Path.GetRandomFileName(), source);
                    zip.Save(zipFileToCreate);
                }

                BasicVerifyZip(zipFileToCreate, password);
            }
        }



        [TestMethod]
        public void WinZipAes_InMemory_wi8493()
        {
            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [WinZipAes_InMemory_wi8493] : winzip is not present");
                return;
            }

            Directory.SetCurrentDirectory(TopLevelDir);

            string password = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

            using (MemoryStream ms = new MemoryStream())
            {
                for (int m = 0; m < 2; m++)
                {
                    string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("WinZipAes_InMemory_wi8493-{0}.zip", m));

                    using (var zip = new ZipFile())
                    {
                        zip.Password = password;
                        zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                        zip.AddEntry(Path.GetRandomFileName(), "Hello, World!");
                        if (m==1)
                            zip.Save(ms);
                        else
                            zip.Save(zipFileToCreate);
                    }

                    if (m==1)
                        File.WriteAllBytes(zipFileToCreate,ms.ToArray());

                    BasicVerifyZip(zipFileToCreate, password);
                }
            }
        }



        [TestMethod]
        public void WinZipAes_InMemory_wi8493a()
        {
            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [WinZipAes_InMemory_wi8493a] : winzip is not present");
                return;
            }

            Directory.SetCurrentDirectory(TopLevelDir);

            string zipFileToCreate = Path.Combine(TopLevelDir, "WinZipAes_InMemory_wi8493a.zip");
            string password = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

            string[] TextFiles = new string[25 + _rnd.Next(8)];
            for (int i = 0; i < TextFiles.Length; i++)
            {
                TextFiles[i] = Path.Combine(TopLevelDir, String.Format("TextFile{0}.txt", i));
                TestUtilities.CreateAndFillFileText(TextFiles[i], _rnd.Next(14000) + 13000);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                using (var zip = new ZipFile())
                {
                    zip.Password = password;
                    zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                    zip.AddEntry("Readme.txt", "Hello, World! ABC ABC ABC ABC ABCDE ABC ABCDEF ABC ABCD");
                    zip.AddFiles(TextFiles, "files");
                    zip.Save(ms);
                }
                File.WriteAllBytes(zipFileToCreate,ms.ToArray());

                BasicVerifyZip(zipFileToCreate, password);
            }
        }



        [TestMethod]
        //[ExpectedException(typeof(System.InvalidOperationException))]
        public void WinZipAes_Update_SwitchCompression()
        {
            if (!WinZipIsPresent)
            {
                TestContext.WriteLine("skipping test [WinZipAes_Update_SwitchCompression] : winzip is not present");
                return;
            }

            Directory.SetCurrentDirectory(TopLevelDir);
            string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("WinZipAes_Update_SwitchCompression.zip"));
            string password = TestUtilities.GenerateRandomPassword();

            TestContext.WriteLine("=======================================");
            TestContext.WriteLine("Creating file {0}", zipFileToCreate);
            TestContext.WriteLine("  Password:   {0}", password);
            int entries = _rnd.Next(21) + 5;

            string filename = null;
            var checksums = new Dictionary<string, string>();
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip1.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                zip1.Password = password;

                for (int i = 0; i < entries; i++)
                {
                    if (_rnd.Next(2) == 1)
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.bin", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileBinary(filename, filesize);
                    }
                    else
                    {
                        filename = Path.Combine(TopLevelDir, String.Format("Data{0}.txt", i));
                        int filesize = _rnd.Next(144000) + 5000;
                        TestUtilities.CreateAndFillFileText(filename, filesize);
                    }
                    zip1.AddFile(filename, "");

                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                }

                zip1.Comment = String.Format("This archive uses Encryption({0}) password({1}) no compression.", zip1.Encryption, password);
                TestContext.WriteLine("{0}", zip1.Comment);
                TestContext.WriteLine("Saving the zip...");
                zip1.Save(zipFileToCreate);
            }

            BasicVerifyZip(zipFileToCreate, password);

            TestContext.WriteLine("=======================================");
            TestContext.WriteLine("Updating the zip file");

            // Update the zip file
            using (ZipFile zip = ZipFile.Read(zipFileToCreate))
            {
                for (int j = 0; j < 5; j++)
                {
                    zip[j].Password = password;
                    zip[j].CompressionMethod = 0;
                }
                zip.Save(); // this should succeed
            }

        }


    }
}
