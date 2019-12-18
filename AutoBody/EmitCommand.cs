using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AnylandMods.AutoBody {
    public static class EmitCommand {
        public static void Emit(Transform source, string cmdline)
        {
            string[] words = cmdline.ToLower().Split(' ');

            var thingName = new StringBuilder();
            Vector3 velocity = Vector3.zero;
            Vector3 angularVelocity = Vector3.zero;
            Vector3 offset = Vector3.zero;
            Vector3 projectDir = Vector3.zero;
            float defaultProjectDist = 0;
            bool lookingForThingName = true;

            for (int i=0; i<words.Length;) {
                string word = words[i];
                int extraWordsToAdvance = 0;
                if (word.StartsWith("-v")) {
                    extraWordsToAdvance = ParseVector(words, i + 1, word.Substring(2), source, out velocity);
                    lookingForThingName = false;
                } else if (word.StartsWith("-a")) {
                    extraWordsToAdvance = ParseVector(words, i + 1, word.Substring(2), source, out angularVelocity);
                    lookingForThingName = false;
                } else if (word.StartsWith("-o")) {
                    extraWordsToAdvance = ParseVector(words, i + 1, word.Substring(2), source, out offset);
                    lookingForThingName = false;
                } else if (word.StartsWith("-p")) {
                    extraWordsToAdvance = ParseVector(words, i + 1, word.Substring(2), source, out projectDir);
                    lookingForThingName = false;
                } else if (word.Equals("-d")) {
                    float.TryParse(words[i + 1], out defaultProjectDist);
                } else if (lookingForThingName) {
                    if (thingName.Length > 0)
                        thingName.Append(' ');
                    thingName.Append(word);
                }

                if (!lookingForThingName && thingName.Length == 0) {
                    lookingForThingName = true;
                }

                if (extraWordsToAdvance == -1) {
                    break;
                } else {
                    i += 1 + extraWordsToAdvance;
                }
            }

            Vector3 position = source.position;
            if (projectDir.magnitude > 0) {
                RaycastHit[] hits = Physics.RaycastAll(source.position, projectDir);
                try {
                    RaycastHit hit = hits.First(h => h.collider.gameObject != source.gameObject);
                    position = hit.point;
                } catch (InvalidOperationException) {
                    position += defaultProjectDist * projectDir;
                }
            }
            position += offset;
            Quaternion rotation = source.rotation;
            if (velocity.magnitude > 0) {
                rotation = Quaternion.FromToRotation(Vector3.forward, velocity.normalized);
            }

            SyncTools.SpawnThing(Main.config.Emittables[thingName.ToString().ToLower()].thingId, position, rotation, velocity, angularVelocity);
        }

        private static int ParseVector(string[] words, int index, string flags, Transform localBase, out Vector3 vector)
        {
            Vector3? singleCoord = null;
            if (flags.Contains("x")) {
                singleCoord = Vector3.right;
            } else if (flags.Contains("y")) {
                singleCoord = Vector3.up;
            } else if (flags.Contains("z")) {
                singleCoord = Vector3.forward;
            }

            Vector3 givenVector;

            try {
                float x = float.Parse(words[index]);
                if (singleCoord.HasValue) {
                    givenVector = singleCoord.Value * x;
                } else {
                    float y = float.Parse(words[index + 1]);
                    float z = float.Parse(words[index + 2]);
                    givenVector = new Vector3(x, y, z);
                }

                if (flags.Contains("l")) {
                    vector = localBase.position + localBase.rotation * givenVector;
                } else {
                    vector = localBase.position + givenVector;
                }

                return singleCoord.HasValue ? 1 : 3;
            } catch (FormatException) {
                vector = Vector3.zero;
                if (singleCoord.HasValue) {
                    DebugLog.Log("Error: '{0}' is not a valid number.", words[index]);
                    return 1;
                } else {
                    DebugLog.Log("Error: Invalid number in '{0} {1} {2}'.", words[index], words[index + 1], words[index + 2]);
                    return 3;
                }
            } catch (IndexOutOfRangeException) {
                vector = Vector3.zero;
                DebugLog.Log("Error: not enough arguments given to '{0}'.", words[index - 1]);
                return -1;
            }
        }
    }
}
