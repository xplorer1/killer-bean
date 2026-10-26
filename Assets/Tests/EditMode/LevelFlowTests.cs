using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BeanBlaster.Combat;
using BeanBlaster.Core;
using BeanBlaster.Economy;
using BeanBlaster.Level;
using BeanBlaster.Save;
using NUnit.Framework;
using UnityEngine;

namespace BeanBlaster.Tests.EditMode
{
    public sealed class LevelFlowTests
    {
        private string directory;
        private GameConfig config;
        private WeaponDatabase database;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "BeanBlasterLevelFlowTests", System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            SaveSystem.SetSaveDirectoryForTests(directory);
            SaveSystem.SetValidationDataForTests(new Dictionary<string, int> { ["pistol_dual"] = int.MaxValue }, 3);
            config = ScriptableObject.CreateInstance<GameConfig>();
            SetField(config, "levelSceneNames", new List<string> { "Level_01", "Level_02", "Level_03" });
            database = ScriptableObject.CreateInstance<WeaponDatabase>();
            EconomyService.Initialize(database, config);
            SaveSystem.Load();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(database);
            SaveSystem.ClearTestOverrides();
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }

        [Test]
        public void CompletionAwardsOnceAndPersistsRecordAndNextUnlock()
        {
            GameObject managerObject = new GameObject("Level Manager");
            LevelManager manager = managerObject.AddComponent<LevelManager>();
            SetField(manager, "config", config);
            SetField(manager, "levelIndex", 1);
            manager.AdvanceAttempt(12.5f);
            int events = 0;
            manager.LevelCompleted += _ => events++;

            Assert.That(manager.CompleteLevel(), Is.True);
            Assert.That(manager.CompleteLevel(), Is.False);

            Assert.That(SaveSystem.Current.coins, Is.EqualTo(1200));
            Assert.That(events, Is.EqualTo(1));
            Assert.That(SaveSystem.Current.levelProgress.highestUnlockedLevel, Is.EqualTo(2));
            LevelRecord record = SaveSystem.Current.GetOrCreateRecord(1);
            Assert.That(record.completed, Is.True);
            Assert.That(record.noDeathClear, Is.True);
            Assert.That(record.bestTimeSeconds, Is.EqualTo(12.5f));
            Assert.That(SaveSystem.IsDirty, Is.False);
            Object.DestroyImmediate(managerObject);
        }

        [Test]
        public void PauseFreezesAttemptTimerAndRestoresTimeScale()
        {
            GameObject managerObject = new GameObject("Level Manager");
            LevelManager manager = managerObject.AddComponent<LevelManager>();
            manager.AdvanceAttempt(1f);
            manager.SetPaused(true);
            manager.AdvanceAttempt(5f);
            Assert.That(manager.AttemptTime, Is.EqualTo(1f));
            Assert.That(Time.timeScale, Is.Zero);
            manager.SetPaused(false);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Object.DestroyImmediate(managerObject);
        }

        [Test]
        public void AutosaveWaitsForIntervalAndWritesOnlyWhenDirty()
        {
            GameObject serviceObject = new GameObject("Save Auto Flush");
            SaveAutoFlush flush = serviceObject.AddComponent<SaveAutoFlush>();
            SetField(flush, "interval", 1f);
            Assert.That(flush.Advance(1f), Is.False);
            SaveSystem.Current.coins = 42;
            SaveSystem.MarkDirty();
            Assert.That(flush.Advance(0.5f), Is.False);
            Assert.That(flush.Advance(0.5f), Is.True);
            Assert.That(SaveSystem.IsDirty, Is.False);
            Object.DestroyImmediate(serviceObject);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(target, value);
        }
    }
}
