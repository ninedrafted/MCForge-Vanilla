﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCForge.Utils;
using MCForge.World;
using System.IO;
using System.Threading;
using MCForge.Utils.Settings;

namespace MCForge.Core {
    public class Backup {

        internal static string DateFormat {
            get { return _lastTime.ToString("MM-dd-yyyy"); }
        }

        private static DateTime _lastTime;
        private static Timer _timer;

        public static void BackupAll() {
            Logger.Log("Backed up at " + DateTime.Now.ToString("T"));
            CheckDirs();

            BackupLevels();
            BackupGroups();
        }

        public static void BackupLevels() {
            CheckDirs();

            foreach (var level in Level.Levels) {
                if (!level.BackupLevel)
                    continue;

                BackupLevel(level);
            }
        }

        public static void BackupGroups() {
            CheckDirs();
        }


        public static void BackupLevel(Level s, string suffix = "") {
            BackupLevel(s.Name, suffix);
        }

        public static void BackupLevel(string p, string suffix = "") {
            CheckDirs();

            if (File.Exists(FileUtils.LevelsPath + p + ".lvl")) {
                File.Copy(FileUtils.LevelsPath + p + ".lvl", FileUtils.BackupsPath + DateFormat + "-backup/" + DateTime.Now.ToString("hh-mm-ss-") + p + ".lvl"+ suffix);
            }
        }

        public static void CheckDirs() {
            if (_lastTime.Day != DateTime.Now.Day)
                return;

            _lastTime = DateTime.Now;
            FileUtils.CreateDirIfNotExist(FileUtils.BackupsPath + DateFormat + "-backup");
        }

        public static void StartBackup() {
            if (!ServerSettings.GetSettingBoolean("BackupFiles"))
                return;

            _timer = new Timer((e) => { BackupAll(); }, null, 0, ServerSettings.GetSettingInt("BackupInterval") * 1000);
        }
        static Backup() {
            _lastTime = DateTime.Now;
            FileUtils.CreateDirIfNotExist(FileUtils.BackupsPath + DateFormat + "-backup");

        }


    }
}
