using System;
using System.Linq;
using Clio.Utilities.Helpers;
using ff14bot;
using ff14bot.AClasses;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;

namespace Anthem
{
    /// <summary>
    /// It's called Anthem because it's not as good as Destiny.
    /// </summary>
    public class Anthem : BotPlugin
    {
        public override string Author => @"Freiheit";
        public override Version Version => new Version(1, 0, 0);
        public override string Name => @"Anthem";

        // Change this value to adjust the number of levels to work on!
        private readonly ushort _maxLevelDifference = 10;

        // Maximum level, usually 70, 60 or 50.
        private readonly ushort _maxLevel = 70;

        private readonly WaitTimer _waitTimer = new WaitTimer(TimeSpan.Zero);

        private ClassJobType _jobToLevel = ClassJobType.Adventurer;
        private ushort _jobStartLevel;

        public override void OnPulse()
        {
            if (!_waitTimer.IsFinished)
                return;

            if (IsBusyOrDone())
            {
                ResetWaitTimer();
                return;
            }

            if (_jobToLevel == ClassJobType.Adventurer)
                SelectNewJobToLevel();

            if (Core.Player.CurrentJob != _jobToLevel)
            {
                SwitchGearset(_jobToLevel);
                return;
            }

            if (Core.Player.CurrentJob == _jobToLevel &&
                (Core.Player.ClassLevel >= _jobStartLevel + _maxLevelDifference || Core.Player.ClassLevel >= _maxLevel))
            {
                SelectNewJobToLevel();
                return;
            }

            ResetWaitTimer();
        }

        /// <summary>
        /// Consolidate all the busy/done checks
        /// </summary>
        /// <returns></returns>
        private bool IsBusyOrDone()
        {
            // Standard busy checks...
            if (Core.Player.InCombat || Core.Player.IsDead || Core.Player.IsDying || CommonBehaviors.IsLoading)
                return true;

            // Ignore crafters, gatherers and Blue Mage
            if (!IsBattleClassJob(Core.Player.CurrentJob))
                return true;

            // This check also fires when in The Wolfs Den!
            if (WorldManager.InPvP)
                return true;

            // Are we in some sort of special event like Gold Saucer, a dungeon or an instance?
            if (DirectorManager.ActiveDirector != null)
                return true;

            // Don't make the bot switch jobs when we are already done leveling
            if (IsDoneLeveling())
                return true;

            return false;
        }

        /// <summary>
        /// Resets the wait timer
        /// </summary>
        private void ResetWaitTimer()
        {
            _waitTimer.WaitTime = new TimeSpan(0, 0, 1);
            _waitTimer.Reset();
        }

        /// <summary>
        /// Selects a new job to level
        /// </summary>
        private void SelectNewJobToLevel()
        {
            _jobToLevel = SelectLowestLevelClassJob();
            _jobStartLevel = Core.Me.Levels.FirstOrDefault(j => j.Key == _jobToLevel).Value;

            var maxLevel = _jobStartLevel + _maxLevelDifference;

            Logging.WriteVerbose($"Will be leveling {_jobToLevel} up to level {maxLevel} next.");
        }

        /// <summary>
        /// Switch to the first gearset for a given class/job
        /// </summary>
        /// <param name="targetJob"></param>
        private void SwitchGearset(ClassJobType targetJob)
        {
            var jobGearset = GearsetManager.GearSets.FirstOrDefault(g => g.Class == targetJob);

            if (jobGearset.Index > 0)
            {
                Logging.WriteVerbose($"Switching to gearset {jobGearset.Index} for {targetJob}...");
                jobGearset.Activate();
            }
        }

        /// <summary>
        /// Selects the lowest level class/job
        /// </summary>
        /// <returns></returns>
        private ClassJobType SelectLowestLevelClassJob()
        {
            return Core.Player.Levels
                .Where(j => j.Value >= 1 && j.Value < _maxLevel)
                .Where(j => IsBattleClassJob(j.Key))
                .OrderBy(j => j.Value)
                .FirstOrDefault(j => HasGearset(j.Key))
                .Key;
        }

        /// <summary>
        /// Helper method to evaluate whether all relevant jobs have finished leveling.
        /// </summary>
        /// <returns></returns>
        private bool IsDoneLeveling()
        {
            return !Core.Player.Levels.Any(j => HasFinishedLevelingClassJob(j.Key, _maxLevel));
        }

        /// <summary>
        /// Checks whether we have a gearset for the given class/job.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        private bool HasGearset(ClassJobType job)
        {
            return GearsetManager.GearSets.Any(g => g.Class == job);
        }

        /// <summary>
        /// Helper method to check whether a class/job is done leveling completely.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        private bool HasFinishedLevelingClassJob(ClassJobType job, ushort maxLevel)
        {
            var level = Core.Player.Levels[job];
            return level < 1 || level >= maxLevel;
        }

        /// <summary>
        /// Returns whether a given job is valid for this plugin.
        /// All Disciples of War and Disciples of Magic, except Blue Mage.
        /// </summary>
        /// <param name="job"></param>
        /// <returns></returns>
        private bool IsBattleClassJob(ClassJobType job)
        {
            return job == ClassJobType.Arcanist ||
                   job == ClassJobType.Archer ||
                   job == ClassJobType.Astrologian ||
                   job == ClassJobType.Bard ||
                   job == ClassJobType.Conjurer ||
                   job == ClassJobType.Dragoon ||
                   job == ClassJobType.Gladiator ||
                   job == ClassJobType.Lancer ||
                   job == ClassJobType.Machinist ||
                   job == ClassJobType.Marauder ||
                   job == ClassJobType.Monk ||
                   job == ClassJobType.Ninja ||
                   job == ClassJobType.Paladin ||
                   job == ClassJobType.Pugilist ||
                   job == ClassJobType.Rogue ||
                   job == ClassJobType.Samurai ||
                   job == ClassJobType.Scholar ||
                   job == ClassJobType.Summoner ||
                   job == ClassJobType.Thaumaturge ||
                   job == ClassJobType.Warrior ||
                   job == ClassJobType.BlackMage ||
                   job == ClassJobType.DarkKnight ||
                   job == ClassJobType.RedMage ||
                   job == ClassJobType.WhiteMage;
        }
    }
}