using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PurplePen
{
    class RelayVariations
    {
        EventDB eventDB;
        Id<Course> courseId;
        int numberTeams, numberLegs;

        Random random;

        Dictionary<Id<CourseControl>, char> variationMapping;
        Dictionary<string, VariationInfo> variationInfos;

        // Forks in both list and topology form.
        Fork firstForkInCourse;
        List<Fork> allForks;
        int totalPossiblePaths;  // Number of ways through the forks.

        // Each element of the list is a team, with variation strings for each leg.
        List<TeamAssignment> results;
        List<BranchWarning> branchWarnings;

        public RelayVariations(EventDB eventDB, Id<Course> courseId, int numberTeams, int numberLegs)
        {
            this.eventDB = eventDB;
            this.courseId = courseId;
            this.numberTeams = numberTeams;
            this.numberLegs = numberLegs;

            variationInfos = QueryEvent.GetAllVariations(eventDB, courseId).ToDictionary(vi => vi.VariationCodeString);

            Generate();
        }

        // Get the variation to use for a particular team and leg.
        public VariationInfo GetVariation(int team, int leg)
        {
            if (team < 1 || team > numberTeams)
                throw new ArgumentOutOfRangeException("team", "team numbers are from 1 to number of teams");
            if (leg < 1 || leg > numberLegs)
                throw new ArgumentOutOfRangeException("leg", "leg numbers are from 1 to number of legs");

            string variationString = results[team - 1].GetVariationStringForLeg(leg - 1);
            return variationInfos[variationString];
        }

        public int NumberOfTeams
        {
            get { return numberTeams; }
        }

        public int NumberOfLegs
        {
            get { return numberLegs; }
        }

        // Get any warnings about branches that are used unevenly.
        public IEnumerable<BranchWarning> GetBranchWarnings()
        {
            return branchWarnings;
        }

        // Get the total number of different possible variations.
        public int GetTotalPossiblePaths()
        {
            return totalPossiblePaths;
        }

        void Generate()
        {
            // We want the randomness to be determistic.
            results = new List<TeamAssignment>(numberTeams);
            branchWarnings = new List<BranchWarning>();
            random = new Random(8713527);

            // Find all the forks.
            FindForks();

            // Do initial traverse of all forks to get number of runners, warnings about number of people at each branch.
            totalPossiblePaths = ScanFork(firstForkInCourse, numberLegs, 1);
            Debug.Assert(totalPossiblePaths == variationInfos.Count);

            for (int i = 0; i < numberTeams; ++i) {
                results.Add(GenerateTeam());
            }
        }


        private TeamAssignment GenerateTeam()
        {
            // Return a potential team that doesn't duplicate previous teams, or failing that,
            // with the minimum number of duplicates.
            int minCount = int.MaxValue;
            TeamAssignment minTeam = null;

            for (int i = 0; i < 100; ++i) { 
                TeamAssignment team = GeneratePotentialTeam();
                int countDups = results.Count(existingTeam => existingTeam.Equals(team));
                if (countDups == 0) {
                    Debug.WriteLine("Team {0} formed with 0 dups", results.Count);
                    return team;
                }
                if (countDups < minCount) {
                    minCount = countDups;
                    minTeam = team;
                }
            }

            Debug.WriteLine("Team {0} formed with {1} dups", results.Count, minCount);
            return minTeam;
        }

        private TeamAssignment GeneratePotentialTeam()
        {
            TeamAssignment teamAssignment = new TeamAssignment(this);

            for (int leg = 0; leg < numberLegs; ++leg) {
                AddLegToTeamAssignment(leg, teamAssignment);
            }

            return teamAssignment;
        }


        private void AddLegToTeamAssignment(int leg, TeamAssignment teamAssignment)
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
                    return;  // perfect enough to return.
                if (score <= minScore && count > 50)
                    return; // good enough; as good as all previous and more than 50 considered.
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
                // Get the branches remaining to be used for this team.
                List<int> possibleBranches = GetPossibleBranches(fork);
                for (int i = 0; i < leg; ++i) {
                    if (teamAssignment.legAssignForFork[fork].branchForLeg[i].Length > 0)
                        possibleBranches.Remove(teamAssignment.legAssignForFork[fork].branchForLeg[i][0]);
                }
                // Pick a random one.
                int selectedBranch = possibleBranches[random.Next(possibleBranches.Count)];

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

            // Check 1: check again previous teams on same leg.
            int allowedDuplicates = (results.Count / totalPossiblePaths);
            if (allowedDuplicates >= 1) {
                allowedDuplicates += (int)Math.Ceiling((double)allowedDuplicates / 3); // allow some slop after all options used once.
            }
            int duplicates = 0;
            for (int i = 0; i < results.Count; ++i) {
                if (results[i].LegEquals(leg, teamAssignment, leg))
                    ++duplicates;
            }

            score += Math.Max(0, (duplicates - allowedDuplicates));

            if (numberLegs <= totalPossiblePaths) {
                // Check 2: check against previous legs on same team, if they should be unique
                for (int otherLeg = 0; otherLeg < leg; ++otherLeg) {
                    if (teamAssignment.LegEquals(leg, teamAssignment, otherLeg))
                        score += 10;
                }
            }

            return score;
        }

        List<int> GetPossibleBranches(Fork fork)
        {
            List<int> result = new List<int>();
            int branch = 0;
            for (int i = 0; i < fork.numLegsHere; ++i) {
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
                // Figure out how many legs per branch. May not be even.
                int branchesMore = numLegsOnThisFork % startFork.numBranches;
                int legsPerBranch = numLegsOnThisFork / startFork.numBranches;

                if (branchesMore != 0) {
                    // The number of branches doesn't evenly divide the number of legs that start here. Given a warning.
                    branchWarnings.Add(new BranchWarning(startFork.controlCode, legsPerBranch + 1, startFork.codes.Take(branchesMore),
                                                                                legsPerBranch, startFork.codes.Skip(branchesMore)));
                }

                int waysThroughBranches = 0;

                for (int i = 0; i < startFork.numBranches; ++i) {
                    int legsThisBranch = legsPerBranch;
                    if (i < branchesMore)
                        ++legsThisBranch;

                    waysThroughBranches += ScanFork(startFork.subForks[i], legsThisBranch, totalPathsToThisPoint);
                }

                return ScanFork(startFork.next, numLegsOnThisFork, waysThroughBranches);
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
                    currentFork.controlCode = eventDB.GetControl(courseCtl.control).code;
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
            public char[] codes;     // codes for the branches.

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
                    int[] thisForkLeg = legAssignForFork[fork].branchForLeg[leg];
                    int[] otherForkLeg = other.legAssignForFork[fork].branchForLeg[otherLeg];
                    if (thisForkLeg.Length != otherForkLeg.Length)
                        return false;
                    for (int j = 0; j < thisForkLeg.Length; ++j) {
                        if (thisForkLeg[j] != otherForkLeg[j])
                            return false;
                    }
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
}
