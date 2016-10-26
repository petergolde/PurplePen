using System;
using System.Collections.Generic;
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

        // Forks in both list and topology form.
        Fork firstForkInCourse;
        List<Fork> allForks;
        long totalPossiblePaths;  // Number of ways through the forks.

        // Each element of the list is a team, with variation strings for each leg.
        List<string[]> results;
        List<BranchWarning> branchWarnings;

        public RelayVariations(EventDB eventDB, Id<Course> courseId, int numberTeams, int numberLegs)
        {
            this.eventDB = eventDB;
            this.courseId = courseId;
            this.numberTeams = numberTeams;
            this.numberLegs = numberLegs;

            Generate();
        }

        public List<string[]> GetLegAssignments()
        {
            return results;
        }

        public IEnumerable<BranchWarning> GetBranchWarnings()
        {
            return branchWarnings;
        }

        public long GetTotalPossiblePaths()
        {
            return totalPossiblePaths;
        }

        void Generate()
        {
            // We want the randomness to be determistic.
            results = new List<string[]>(numberTeams);
            branchWarnings = new List<BranchWarning>();
            random = new Random(8713527);

            // Find all the forks.
            FindForks();

            // Do initial traverse of all forks to get number of runners, warnings about number of people at each branch.
            totalPossiblePaths = ScanFork(firstForkInCourse, numberTeams, 1);

            for (int i = 0; i < numberTeams; ++i) {
                results.Add(GenerateTeam());
            }
        }


        class LegAssignment
        {
            public List<int[]> branchForLeg = new List<int[]>();  // branchForLeg[leg] = branch index 0..numBranches-1
        }

        private string[] GenerateTeam()
        {
            LegAssignment[] teamAssignment = new LegAssignment[allForks.Count];
            for (int i = 0; i < teamAssignment.Length; ++i)
                teamAssignment[i] = new LegAssignment();

            for (int leg = 0; leg < numberLegs; ++leg) {
                AddLegToTeamAssignment(leg, teamAssignment);
            }

            List<string> legs = new List<string>();
            for (int leg = 0; leg < numberLegs; ++leg) {
                legs.Add(ConvertTeamAssignmentToString(teamAssignment, leg));
            }

            return legs.ToArray();
        }

        private string ConvertTeamAssignmentToString(LegAssignment[] teamAssignment, int leg)
        {
            // Go throught the forks and add to string.
            StringBuilder builder = new StringBuilder();
            AddForkToStringBuilder(firstForkInCourse, builder, teamAssignment, leg);
            return builder.ToString();
        }

        private void AddForkToStringBuilder(Fork fork, StringBuilder builder, LegAssignment[] teamAssignment, int leg)
        {
            if (fork == null)
                return;

            int forkIndex = allForks.IndexOf(fork);
            int[] selectedBranch = teamAssignment[forkIndex].branchForLeg[leg];
            for (int i = 0; i < selectedBranch.Length; ++i) {
                builder.Append(fork.codes[selectedBranch[i]]);
                AddForkToStringBuilder(fork.subForks[selectedBranch[i]], builder, teamAssignment, leg);
            }

            AddForkToStringBuilder(fork.next, builder, teamAssignment, leg);
        }

        private void AddLegToTeamAssignment(int leg, LegAssignment[] teamAssignment)
        {
            for (int count = 0; ; ++count) {
                for (int forkIndex = 0; forkIndex < allForks.Count; ++forkIndex) {
                    AddForkToTeamAssignment(forkIndex, leg, teamAssignment);
                }

                if (ValidateForkAssignment(leg, teamAssignment) || count >= 10000)
                    return;
                else {
                    for (int forkIndex = 0; forkIndex < allForks.Count; ++forkIndex) {
                        RemoveForkFromTeamAssignment(forkIndex, leg, teamAssignment);
                    }
                }
            }
        }

        private void AddForkToTeamAssignment(int forkIndex, int leg, LegAssignment[] teamAssignment)
        {
            Fork fork = allForks[forkIndex];

            if (fork.loop) {
                // TODO.
                throw new NotImplementedException();
            }
            else {
                // Get the branches remaining to be used for this team.
                List<int> possibleBranches = GetPossibleBranches(fork);
                for (int i = 0; i < leg; ++i)
                    possibleBranches.Remove(teamAssignment[forkIndex].branchForLeg[i][0]);

                // Pick a random one.
                int selectedBranch = possibleBranches[random.Next(possibleBranches.Count)];

                // Store it.
                teamAssignment[forkIndex].branchForLeg.Add(new int[1] { selectedBranch });
            }
        }

        private void RemoveForkFromTeamAssignment(int forkIndex, int leg, LegAssignment[] teamAssignment)
        {
            teamAssignment[forkIndex].branchForLeg.RemoveAt(teamAssignment[forkIndex].branchForLeg.Count - 1);
        }

        private bool ValidateForkAssignment(int leg, LegAssignment[] teamAssignment)
        {
            // UNDONE.
            return true;
        }

        List<int> GetPossibleBranches(Fork fork)
        {
            List<int> result = new List<int>();
            int branch = 0;
            for (int i = 0; i < numberLegs; ++i) {
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
        private long ScanFork(Fork startFork, int numLegsOnThisFork, long totalPathsToThisPoint)
        {
            if (startFork == null)
                return totalPathsToThisPoint;

            startFork.numLegsHere = numLegsOnThisFork;

            if (startFork.loop) {
                long waysThroughLoops = totalPathsToThisPoint;

                for (int i = 0; i < startFork.numBranches; ++i) {
                    waysThroughLoops *= ScanFork(startFork.subForks[i], numLegsOnThisFork, 1);
                }

                waysThroughLoops *= Util.Factorial(startFork.numBranches);

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

                long waysThroughBranches = 0;

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

        T[] RandomShuffle<T>(IEnumerable<T> collection)
        {
            // We have to copy all items anyway, and there isn't a way to produce the items
            // on the fly that is linear. So copying to an array and shuffling it is an efficient as we can get.
            T[] array = collection.ToArray();

            int count = array.Length;
            for (int i = count - 1; i >= 1; --i) {
                // Pick an random number 0 through i inclusive.
                int j = random.Next(i + 1);

                // Swap array[i] and array[j]
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }

            return array;
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


    }
}
