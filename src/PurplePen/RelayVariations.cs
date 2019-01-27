using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PurplePen
{
    class RelayVariations
    {
        EventDB eventDB;
        Id<Course> courseId;
        int firstTeamNumber, numberTeams, numberLegs;
        FixedBranchAssignments fixedBranchAssignments;

        Random random;

        Dictionary<Id<CourseControl>, char> variationMapping;
        Dictionary<string, VariationInfo.VariationPath> variationPaths;

        bool forksScanned = false;
        bool teamsGenerated = false;

        // Forks in both list and topology form.
        Fork firstForkInCourse;
        List<Fork> allForks;
        int totalPossiblePaths;  // Number of ways through the forks.
        int[] minUniquePathsByLeg;  // minimum number of unique ways, given a particular leg.

        // Each element of the list is a team, with variation strings for each leg.
        List<TeamAssignment> results;
        List<BranchWarning> branchWarnings;

        public RelayVariations(EventDB eventDB, Id<Course> courseId, RelaySettings relaySettings)
        {
            this.eventDB = eventDB;
            this.courseId = courseId;
            this.firstTeamNumber = relaySettings.firstTeamNumber;
            this.numberTeams = relaySettings.relayTeams;
            this.numberLegs = relaySettings.relayLegs;

            if (!relaySettings.relayBranchAssignments.IsEmpty) {
                this.fixedBranchAssignments = ValidateFixedBranches(relaySettings.relayBranchAssignments);
            }
            else {
                this.fixedBranchAssignments = relaySettings.relayBranchAssignments;
            }

            forksScanned = false;
        }

        // Get the variation to use for a particular team and leg.
        public VariationInfo GetVariation(int team, int leg)
        {
            GenerateAllTeams();

            if (team < firstTeamNumber || team >= firstTeamNumber + numberTeams)
                throw new ArgumentOutOfRangeException("team", string.Format("team numbers are from {0} to {1}", firstTeamNumber, firstTeamNumber + numberTeams - 1));
            if (leg < 1 || leg > numberLegs)
                throw new ArgumentOutOfRangeException("leg", "leg numbers are from 1 to number of legs");

            string variationString = results[team - firstTeamNumber].GetVariationStringForLeg(leg - 1);
            return new VariationInfo(variationString, variationPaths[variationString], team, leg);
        }

        public int FirstTeamNumber {
            get { return firstTeamNumber; }
        }

        public int LastTeamNumber {
            get { return firstTeamNumber + numberTeams - 1; }
        }

        public int NumberOfTeams
        {
            get { return numberTeams; }
        }

        public int NumberOfLegs
        {
            get { return numberLegs; }
        }

        // Get sets of branches that can be fixed to specific legs.
        public List<char[]> GetPossibleFixedBranches()
        {
            List<char[]> result = new List<char[]>();

            ScanAllForks();

            foreach (Fork fork in allForks)
            {
                if (! fork.loop && fork.numLegsHere == numberLegs)
                {
                    result.Add(fork.codes);
                }
            }

            return result;
        }

        // Validate the fixed branches, and create a new FixedBranchAssignments that doesn't
        // have errors in it.
        public FixedBranchAssignments ValidateFixedBranches(FixedBranchAssignments assignments)
        {
            List<string> dummy;
            return ValidateFixedBranches(assignments, out dummy);
        }

        public FixedBranchAssignments ValidateFixedBranches(FixedBranchAssignments assignments, out List<string> errors)
        {
            FixedBranchAssignments result = new FixedBranchAssignments();
            errors = new List<string>();

            ScanAllForks();
            foreach (Fork fork in allForks) {
                if (!fork.loop && fork.numLegsHere == numberLegs) {
                    // This fork can have fixed branches.
                    ValidateFixedBranchesForFork(assignments, fork, result, errors);
                }
                else {
                    // This fork can't have branches. We don't give errors for this right now, but could here.
                }

            }

            return result;
        }

        private void ValidateFixedBranchesForFork(FixedBranchAssignments assignments, Fork fork, FixedBranchAssignments result, List<string> errors)
        {
            char[] codeForLeg = new char[numberLegs];

            foreach (char code in fork.codes) {
                if (assignments.BranchIsFixed(code)) {
                    foreach (int leg in assignments.FixedLegsForBranch(code)) {
                        if (leg < 0 || leg >= numberLegs) {
                            errors.Add(string.Format(MiscText.BadLegNumber, leg + 1, code));
                        }
                        else if (codeForLeg[leg] != 0) {
                            errors.Add(string.Format(MiscText.LegUsedTwice, leg + 1, codeForLeg[leg], code));
                        }
                        else {
                            codeForLeg[leg] = code;
                        }
                    }
                }
            }

            bool allLegsAssigned = codeForLeg.All(c => (c != 0));
            bool allBranchesAssigned = fork.codes.All(c => assignments.BranchIsFixed(c));

            if (allBranchesAssigned && !allLegsAssigned) {
                string allCodes = string.Join(", ", from c in fork.codes select c.ToString());
                for (int leg = 0; leg < numberLegs; ++leg) {
                    if (codeForLeg[leg] == 0)
                        errors.Add(string.Format(MiscText.LegNotAssigned, leg + 1, allCodes));
                }
            }
            else {
                for (int leg = 0; leg < numberLegs; ++leg) {
                    if (codeForLeg[leg] != 0)
                        result.AddBranchAssignment(codeForLeg[leg], leg);
                }
            }
        }

        // Get any warnings about branches that are used unevenly.
        public IEnumerable<BranchWarning> GetBranchWarnings()
        {
            ScanAllForks();

            return branchWarnings;
        }

        // Get the total number of different possible variations.
        public int GetTotalPossiblePaths()
        {
            ScanAllForks();

            return totalPossiblePaths;
        }

        void ScanAllForks()
        {
            if (!forksScanned) {
                // We want the randomness to be determistic.
                results = new List<TeamAssignment>(numberTeams);
                branchWarnings = new List<BranchWarning>();
                random = new Random(8713527);

                // Find all the forks.
                FindForks();

                // Do initial traverse of all forks to get number of runners, warnings about number of people at each branch.
                totalPossiblePaths = ScanFork(firstForkInCourse, numberLegs, 1);

                // Determine minimum unique paths, per leg.
                minUniquePathsByLeg = new int[numberLegs];
                for (int leg = 0; leg < numberLegs; ++leg) {
                    minUniquePathsByLeg[leg] = CalcMinUniquePaths(firstForkInCourse, leg, 1);
                }

                forksScanned = true;
            }
        }

        void GenerateAllTeams()
        {
            if (!teamsGenerated) {
                ScanAllForks();

                variationPaths = QueryEvent.GetAllVariations(eventDB, courseId).ToDictionary(vi => vi.CodeString, vi => vi.Path);
                Debug.Assert(totalPossiblePaths == variationPaths.Count);

                for (int i = 0; i < numberTeams; ++i) {
                    results.Add(GenerateTeam());
                }

                teamsGenerated = true;
            }
        }


        private TeamAssignment GenerateTeam()
        {
            // Return a potential team that doesn't duplicate previous teams, or failing that,
            // with the minimum number of duplicates.
            int minDupCount = int.MaxValue;
            int minTotalScore = int.MaxValue;
            TeamAssignment minTeam = null;

            // Try 100 teams, first prioritizing number of complete duplicates, then
            // prioritizing total score.
            for (int i = 0; i < 100; ++i) { 
                TeamAssignment team = GeneratePotentialTeam();
                int countDups = results.Count(existingTeam => existingTeam.Equals(team));

                if (countDups < minDupCount) {
                    // duplicates takes priority always.
                    minDupCount = countDups;
                    minTotalScore = team.totalScore;
                    minTeam = team;
                }
                else if (countDups == minDupCount && team.totalScore <= minTotalScore) {
                    // if # duplicates is the same, then prioritize by score.
                    if (team.totalScore == minTotalScore && i >= 10)
                        return team;
                    minTotalScore = team.totalScore;
                    minTeam = team;
                }

                if (countDups == 0 && team.totalScore == 0)
                    return team;  // can't be better than 0 on both measures.
                if (countDups == 0 && i > 25)
                    break;
            }

            Debug.WriteLine("Team {0} formed with {1} dups, totalScore of {2}", results.Count, minDupCount, minTeam.totalScore);
            return minTeam;
        }

        private TeamAssignment GeneratePotentialTeam()
        {
            TeamAssignment teamAssignment = new TeamAssignment(this);
            teamAssignment.totalScore = 0;

            for (int leg = 0; leg < numberLegs; ++leg) {
                teamAssignment.totalScore += AddLegToTeamAssignment(leg, teamAssignment);
            }

            return teamAssignment;
        }

        // Add a leg to the given assignment with a low score (for that leg), and return the score assigned for that leg.
        private int AddLegToTeamAssignment(int leg, TeamAssignment teamAssignment)
        {
            int minScore = int.MaxValue;

            for (int count = 0; ; ++count) {
                AddForkToTeamAssignment(firstForkInCourse, leg, teamAssignment);

                // Add empty branch for forks that are not hit in this leg.
                foreach (Fork fork in allForks) {
                    if (teamAssignment.legAssignForFork[fork].branchForLeg.Count == leg)
                        teamAssignment.legAssignForFork[fork].branchForLeg.Add(new int[0]);
                }

                int score = ScoreLegAssignment(leg, teamAssignment);

                if (score == 0)
                    return score;  // perfect enough to return.
                if (score <= minScore && count > 20)
                    return score; // good enough; as good as all previous and more than 20 considered.
                if (score <= minScore * 4 / 3 && count > 50)
                    return score;
                if (score <= minScore * 2 && count > 75)
                    return score;
                if (count > 100)
                    return score;

                minScore = Math.Min(minScore, score);

                foreach (Fork fork in allForks) {
                    RemoveForkFromTeamAssignment(fork, leg, teamAssignment);
                }
            }
        }

        private void AddForkToTeamAssignment(Fork fork, int leg, TeamAssignment teamAssignment)
        {
            if (fork == null)
                return;

            if (fork.loop) {
                int count = 0;
                int[] selectedLoop;
                do {
                    selectedLoop = RandomLoop(fork.numBranches);
                    ++count;
                } while (count < 200 && !ValidateLoopAssignment(selectedLoop, fork, leg, teamAssignment));

                teamAssignment.legAssignForFork[fork].branchForLeg.Add(selectedLoop);

                // All subforks are reached.
                foreach (Fork subFork in fork.subForks)
                    AddForkToTeamAssignment(subFork, leg, teamAssignment);
            }
            else {
                int selectedBranch;

                if (fork.fixedLegs != null && fork.fixedLegs[leg] >= 0) {
                    // This is a fixed leg.
                    selectedBranch = fork.fixedLegs[leg];
                }
                else {
                    // Get the branches remaining to be used for this team.
                    List<int> possibleBranches = GetPossibleBranches(fork);
                    for (int i = 0; i < leg; ++i) {
                        if ((fork.fixedLegs == null || fork.fixedLegs[i] < 0) && teamAssignment.legAssignForFork[fork].branchForLeg[i].Length > 0)
                            possibleBranches.Remove(teamAssignment.legAssignForFork[fork].branchForLeg[i][0]);
                    }
                    // Pick a random one.
                    selectedBranch = possibleBranches[random.Next(possibleBranches.Count)];
                }

                // Store it.
                teamAssignment.legAssignForFork[fork].branchForLeg.Add(new int[1] { selectedBranch });

                // Only visit the selected subfork.
                AddForkToTeamAssignment(fork.subForks[selectedBranch], leg, teamAssignment);
            }

            AddForkToTeamAssignment(fork.next, leg, teamAssignment);
        }

        private void RemoveForkFromTeamAssignment(Fork fork, int leg, TeamAssignment teamAssignment)
        {
            teamAssignment.legAssignForFork[fork].branchForLeg.RemoveAt(teamAssignment.legAssignForFork[fork].branchForLeg.Count - 1);
        }

        private bool ValidateLoopAssignment(int[] loop, Fork fork, int leg, TeamAssignment teamAssignment)
        {
            // Restriction 1: the first loop must be different for first N legs (N is number of loops)
            if (leg < fork.numBranches) {
                for (int otherLeg = 0; otherLeg < leg; ++otherLeg) {
                    int[] otherBranches = teamAssignment.legAssignForFork[fork].branchForLeg[otherLeg];
                    if (otherBranches.Length > 0 && otherBranches[0] == loop[0])
                        return false;
                }
            }

            // Restriction 2: the entire loop must be as different as possible.
            int maxDups = leg / (int)Util.Factorial(fork.numBranches);
            int dups = teamAssignment.legAssignForFork[fork].branchForLeg.Count(branch => Util.EqualArrays(branch, loop));
            if (dups > maxDups)
                return false;

            return true;
        }

        // Check to see how much the given leg assignment duplicates previous legs on this team, 
        // or previous team assignments for this leg. Return score (0 = perfect, higher = worse)
        // indicating how good a match.
        private int ScoreLegAssignment(int leg, TeamAssignment teamAssignment)
        {
            int score = 0;

            // Check 1: check agains previous teams on same leg, add penalty if 
            // too many on same branch. Boost penalty for first leg and first fork.
            bool firstFork = true;
            foreach (Fork fork in allForks) {
                if (fork.fixedLegs != null && fork.fixedLegs[leg] >= 0) {
                    continue; // This is a fixed branch; nothing to score.
                }

                int allowedSimilarBranches = (int) Math.Floor(1.17 * ((double)results.Count / fork.numNonFixedBranches));
                int similarBranches = 0;
                for (int i = 0; i < results.Count; ++i) {
                    if (results[i].LegEqualForFork(fork, leg, teamAssignment, leg)) {
                        ++similarBranches;
                    }
                }
                int penalty = Math.Max(0, (similarBranches - allowedSimilarBranches));
                if (firstFork)
                    penalty *= 3;
                if (leg == 0)
                    penalty *= 3;
                score += penalty;
                firstFork = false;
            }

            // Check 2: check again previous teams on same leg.
            int allowedDuplicates = (results.Count / minUniquePathsByLeg[leg]);
            if (allowedDuplicates >= 1) {
                allowedDuplicates += (int)Math.Ceiling((double)allowedDuplicates / 3); // allow some slop after all options used once.
            }
            int duplicates = 0;
            for (int i = 0; i < results.Count; ++i) {
                if (results[i].LegEquals(leg, teamAssignment, leg))
                    ++duplicates;
            }

            score += 10 * Math.Max(0, (duplicates - allowedDuplicates));

            if (numberLegs <= minUniquePathsByLeg[leg]) {
                // Check 3: check against previous legs on same team, if they should be unique
                for (int otherLeg = 0; otherLeg < leg; ++otherLeg) {
                    if (teamAssignment.LegEquals(leg, teamAssignment, otherLeg))
                        score += 100;
                }
            }

            return score;
        }

        List<int> GetPossibleBranches(Fork fork)
        {
            List<int> result = new List<int>();
            int branch = 0;
            for (int i = 0; i < fork.numLegsHere; ++i) {
                if (fork.fixedLegs != null && fork.fixedLegs[i] >= 0)
                    continue;

                // Skip any branches that are fixed to specific legs.
                while (fork.fixedBranches != null && fork.fixedBranches[branch]) {
                    branch = (branch + 1) % fork.numBranches;
                }

                result.Add(branch);

                branch = (branch + 1) % fork.numBranches;
            }
            return result;
        }

        void FindForks()
        {
            variationMapping = QueryEvent.GetVariantCodeMapping(eventDB, new CourseDesignator(courseId));

            // Find all forks.
            allForks = new List<Fork>();
            Id<CourseControl> firstCourseControlId = eventDB.GetCourse(courseId).firstCourseControl;
            firstForkInCourse = FindForksToJoin(firstCourseControlId, Id<CourseControl>.None);

        }

        // Scan forks starting at this fork, updating number of legs and generate warning as needed.
        // Return number of paths through.
        private int ScanFork(Fork startFork, int numLegsOnThisFork, int totalPathsToThisPoint)
        {
            if (startFork == null)
                return totalPathsToThisPoint;

            startFork.numLegsHere = numLegsOnThisFork;

            if (startFork.loop) {
                int waysThroughLoops = totalPathsToThisPoint;

                for (int i = 0; i < startFork.numBranches; ++i) {
                    waysThroughLoops *= ScanFork(startFork.subForks[i], numLegsOnThisFork, 1);
                }

                waysThroughLoops *= (int) Util.Factorial(startFork.numBranches);

                return ScanFork(startFork.next, numLegsOnThisFork, waysThroughLoops);
            }
            else {
                // If there are fixed branches for this fork, put information about this into the Fork object.
                int numUnfixedLegsOnThisFork = numLegsOnThisFork; // may be reduced below.
                startFork.numNonFixedBranches = startFork.numBranches;  // may be reduced below.
                List<char> nonFixedCodes = new List<char>(startFork.codes);
                if (fixedBranchAssignments != null && numLegsOnThisFork == numberLegs)
                {
                    for (int i = 0; i < startFork.numBranches; ++i)
                    {
                        char code = startFork.codes[i];
                        if (fixedBranchAssignments.BranchIsFixed(code))
                        {
                            nonFixedCodes.Remove(code);

                            if (startFork.fixedBranches == null)
                                startFork.fixedBranches = new bool[startFork.numBranches];
                            startFork.fixedBranches[i] = true;

                            if (startFork.fixedLegs == null)
                            {
                                startFork.fixedLegs = new int[numLegsOnThisFork];
                                for (int x = 0; x < startFork.fixedLegs.Length; ++x)
                                    startFork.fixedLegs[x] = -1;
                            }
                            foreach (int leg in fixedBranchAssignments.FixedLegsForBranch(code))
                            {
                                if (leg >= 0 && leg < startFork.fixedLegs.Length) {
                                    startFork.fixedLegs[leg] = i;
                                    numUnfixedLegsOnThisFork -= 1;
                                }
                            }

                            --startFork.numNonFixedBranches;
                        }
                    }
                }

                int branchesMore = 0, legsPerBranch = 0;
                if (startFork.numNonFixedBranches != 0)
                {
                    // Figure out how many legs per branch. May not be even.
                    branchesMore = numUnfixedLegsOnThisFork % startFork.numNonFixedBranches;
                    legsPerBranch = numUnfixedLegsOnThisFork / startFork.numNonFixedBranches;
                }

                if (branchesMore != 0) {
                    // The number of branches doesn't evenly divide the number of legs that start here. Given a warning.
                    branchWarnings.Add(new BranchWarning(startFork.controlCode, legsPerBranch + 1, nonFixedCodes.Take(branchesMore),
                                                                                legsPerBranch, nonFixedCodes.Skip(branchesMore)));
                }

                int waysThroughBranches = 0;

                for (int i = 0; i < startFork.numBranches; ++i) {
                    int legsThisBranch;
                    char code = startFork.codes[i];
                    if (startFork.fixedBranches != null && startFork.fixedBranches[i])
                    {
                        legsThisBranch = fixedBranchAssignments.FixedLegsForBranch(code).Count;
                    }
                    else
                    {
                        legsThisBranch = legsPerBranch;
                        if (nonFixedCodes.IndexOf(code) < branchesMore)
                            ++legsThisBranch;
                    }

                    waysThroughBranches += ScanFork(startFork.subForks[i], legsThisBranch, totalPathsToThisPoint);
                }

                return ScanFork(startFork.next, numLegsOnThisFork, waysThroughBranches);
            }
        }

        // Scan forks starting at this fork, returning smallest number of unique paths.
        private int CalcMinUniquePaths(Fork startFork, int leg, int smallestPathsToThisPoint)
        {
            if (startFork == null)
                return smallestPathsToThisPoint;

            if (startFork.loop) {
                int waysThroughLoops = smallestPathsToThisPoint;

                for (int i = 0; i < startFork.numBranches; ++i) {
                    waysThroughLoops *= CalcMinUniquePaths(startFork.subForks[i], leg, 1);
                }

                waysThroughLoops *= (int)Util.Factorial(startFork.numBranches);

                return CalcMinUniquePaths(startFork.next, leg, waysThroughLoops);
            }
            else {
                if (startFork.fixedLegs != null && startFork.fixedLegs[leg] >= 0)
                {
                    // Fixed leg.
                    int branch = startFork.fixedLegs[leg];
                    int ways = CalcMinUniquePaths(startFork.subForks[branch], leg, smallestPathsToThisPoint);
                    return CalcMinUniquePaths(startFork.next, leg, ways);
                }
                else
                {
                    int minWaysThroughBranches = int.MaxValue;

                    for (int i = 0; i < startFork.numBranches; ++i)
                    {
                        if (startFork.fixedBranches == null || !startFork.fixedBranches[i])
                            minWaysThroughBranches = Math.Min(CalcMinUniquePaths(startFork.subForks[i], leg, smallestPathsToThisPoint), minWaysThroughBranches);
                    }
                    minWaysThroughBranches *= startFork.numNonFixedBranches;

                    return CalcMinUniquePaths(startFork.next, leg, minWaysThroughBranches);
                }
            }
        }



        Fork FindForksToJoin(Id<CourseControl> begin, Id<CourseControl> join)
        {
            Id<CourseControl> nextCourseControlId = begin;
            Fork firstFork = null, lastFork = null;

            // Traverse the course control links.
            while (nextCourseControlId.IsNotNone && nextCourseControlId != join) {
                CourseControl courseCtl = eventDB.GetCourseControl(nextCourseControlId);

                if (courseCtl.split) {
                    // Record information about the fork, and link it in.
                    Fork currentFork = new Fork();
                    currentFork.loop = courseCtl.loop;
                    currentFork.controlCode = Util.ControlPointName(eventDB, courseCtl.control, NameStyle.Medium);
                    currentFork.numBranches = courseCtl.loop ? courseCtl.splitCourseControls.Length - 1 : courseCtl.splitCourseControls.Length;
                    currentFork.codes = new char[currentFork.numBranches];
                    currentFork.subForks = new Fork[currentFork.numBranches];

                    if (firstFork == null) {
                        firstFork = lastFork = currentFork;
                    }
                    else {
                        lastFork.next = currentFork;
                        lastFork = currentFork;
                    }
                    allForks.Add(currentFork);

                    // Follow all of the alternate paths 
                    int j = 0;
                    for (int i = 0; i < courseCtl.splitCourseControls.Length; ++i) {

                        if (!(courseCtl.loop && (i == 0))) {
                            Id<CourseControl> forkStart = courseCtl.splitCourseControls[i];
                            currentFork.codes[j] = variationMapping[courseCtl.splitCourseControls[i]];
                            currentFork.subForks[j] = FindForksToJoin(eventDB.GetCourseControl(forkStart).nextCourseControl, courseCtl.splitEnd);
                            ++j;
                        }
                    }

                    if (!courseCtl.loop) {
                        nextCourseControlId = courseCtl.splitEnd;
                    } else {
                        nextCourseControlId = courseCtl.nextCourseControl;
                    }
                }
                else {
                    nextCourseControlId = courseCtl.nextCourseControl;
                }
            }

            return firstFork;
        }

        int[] RandomLoop(int numLoops)
        {
            int[] loop = new int[numLoops];
            for (int i = 0; i < loop.Length; ++i)
                loop[i] = i;

            for (int i = loop.Length - 1; i >= 1; --i) {
                // Pick an random number 0 through i inclusive.
                int j = random.Next(i + 1);

                // Swap loop[i] and loop[j]
                int temp = loop[i];
                loop[i] = loop[j];
                loop[j] = temp;
            }

            return loop;
        }

        // Represents a fork or loop.
        class Fork
        {
            public string controlCode; // control code at loop start.
            public bool loop;        // Is it a loop?
            public int numLegsHere;  // number of legs that reach the start of this fork.
            public int numBranches;  // number of branches/loops in this fork.
            public int numNonFixedBranches; // number of branches that are not fixed. Same as numBranches unless some branches are fixed.
            public char[] codes;     // codes for the branches.

            // If numNonFixedBranches != numBranches, then these describe how legs are fixed to branches.
            public int[] fixedLegs; // if a leg is fixed to a branch, then fixedLeg[leg] is the branch it is fixed to. Else -1.
            public bool[] fixedBranches;  // array with numBranches elements. Indicates if a branch is fixed to one or more legs.


            public Fork next;        // next fork (after join point)
            public Fork[] subForks;  // first fork along each fork point.
        }

        // Represents a warning about a uneven branch.
        public class BranchWarning
        {
            public readonly string ControlCode;
            public readonly int numMore;
            public readonly char[] codeMore;
            public readonly int numLess;
            public readonly char[] codeLess;

            public BranchWarning(string controlCode, int numMore, IEnumerable<char> codeMore, int numLess, IEnumerable<char> codeLess)
            {
                ControlCode = controlCode;
                this.numMore = numMore;
                this.codeMore = codeMore.ToArray();
                this.numLess = numLess;
                this.codeLess = codeLess.ToArray();
            }
        }

        // Represent one assignment of team to legs on each fork.
        class TeamAssignment
        {
            RelayVariations outer;
            public Dictionary<Fork, LegAssignmentForOneFork> legAssignForFork;
            public int totalScore; // Total score for all forks.

            public TeamAssignment(RelayVariations relayVariations)
            {
                outer = relayVariations;

                legAssignForFork = new Dictionary<Fork, LegAssignmentForOneFork>();
                foreach (Fork fork in outer.allForks) {
                    legAssignForFork[fork] = new LegAssignmentForOneFork();
                }
            }

            override public bool Equals(object obj)
            {
                TeamAssignment other = obj as TeamAssignment;
                if (other == null)
                    return false;

                foreach (Fork fork in outer.allForks) {
                    if (!legAssignForFork[fork].Equals(other.legAssignForFork[fork]))
                        return false;
                }

                return true;
            }

            public bool LegEquals(int leg, TeamAssignment other, int otherLeg)
            {
                foreach (Fork fork in outer.allForks) {
                    if (!LegEqualForFork(fork, leg, other, otherLeg))
                        return false;
                }

                return true;
            }

            public bool LegEqualForFork(Fork fork, int leg, TeamAssignment other, int otherLeg)
            {
                int[] thisForkLeg = legAssignForFork[fork].branchForLeg[leg];
                int[] otherForkLeg = other.legAssignForFork[fork].branchForLeg[otherLeg];
                if (thisForkLeg.Length != otherForkLeg.Length)
                    return false;
                for (int j = 0; j < thisForkLeg.Length; ++j) {
                    if (thisForkLeg[j] != otherForkLeg[j])
                        return false;
                }

                return true;
            }


            public string GetVariationStringForLeg(int leg)
            {
                // Go throught the forks and add to string.
                StringBuilder builder = new StringBuilder();
                AddForkToStringBuilder(outer.firstForkInCourse, builder, leg);
                return builder.ToString();
            }

            private void AddForkToStringBuilder(Fork fork, StringBuilder builder, int leg)
            {
                if (fork == null)
                    return;

                int[] selectedBranch = legAssignForFork[fork].branchForLeg[leg];
                for (int i = 0; i < selectedBranch.Length; ++i) {
                    builder.Append(fork.codes[selectedBranch[i]]);
                    AddForkToStringBuilder(fork.subForks[selectedBranch[i]], builder, leg);
                }

                AddForkToStringBuilder(fork.next, builder, leg);
            }
        }

        // Represents an assignment of legs for a single fork.
        class LegAssignmentForOneFork
        {
            public List<int[]> branchForLeg = new List<int[]>();  // branchForLeg[leg] = branch index 0..numBranches-1

            public override bool Equals(object obj)
            {
                LegAssignmentForOneFork other = obj as LegAssignmentForOneFork;
                if (other == null)
                    return false;

                if (other.branchForLeg.Count != branchForLeg.Count)
                    return false;

                for (int i = 0; i < branchForLeg.Count; ++i)
                    if (!Util.ArrayEquals(other.branchForLeg[i], branchForLeg[i]))
                        return false;

                return true;
            }

        }


    }

    class CsvWriter
    {
        RelayVariations relayVariations;
        TextWriter writer;

        public void WriteCsv(string fileName, RelayVariations relayVariations)
        {
            this.relayVariations = relayVariations;

            using (writer = new StreamWriter(fileName, false, Encoding.ASCII)) {
                WriteHeaderLine();
                WriteTeams();
            }

            writer = null;
            relayVariations = null;
        }

        void WriteHeaderLine()
        {
            writer.Write("Team");
            for (int legNumber = 1; legNumber <= relayVariations.NumberOfLegs; ++legNumber) {
                writer.Write(",Leg {0}", legNumber.ToString(CultureInfo.InvariantCulture));
            }
            writer.WriteLine();
        }
        private void WriteTeams()
        {
            for (int teamNumber = relayVariations.FirstTeamNumber; teamNumber <= relayVariations.LastTeamNumber; ++teamNumber) {
                writer.Write(teamNumber.ToString(CultureInfo.InvariantCulture));
                for (int legNumber = 1; legNumber <= relayVariations.NumberOfLegs; ++legNumber) {
                    writer.Write(",{0}", relayVariations.GetVariation(teamNumber, legNumber).CodeString);
                }
                writer.WriteLine();
            }
        }
    }


}
