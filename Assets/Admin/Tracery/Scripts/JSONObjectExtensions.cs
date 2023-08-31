using System;

namespace UnityTracery {
  /// <summary>
  /// Adds a Random field to JArray which returns a random entry from the array.
  /// Also provides means to seed the randomness.
  /// </summary>
  public static class JSONObjectExtensions {
    private static System.Random random = new System.Random();

    /// <summary>
    /// Get a random entry from a JSONArray or JSONObject.
    /// </summary>
    /// <param name="jsonObject">Self.</param>
    /// <returns>Random entry from a JSONArray or JSONObject, otherwise self.</returns>
    public static JSONObject Random(this JSONObject jsonObject) {
      // Non-container types just return themselves.
      if (!jsonObject.isContainer) {
        return jsonObject;
      }
      if (jsonObject.Count <= 0) {
        return JSONObject.nullJO;
      }

      if (jsonObject.IsArray) {
        return jsonObject[random.Next(jsonObject.Count)];
      } else {
        return jsonObject[jsonObject.keys[random.Next(jsonObject.keys.Count)]];
      }
    }

    public static JSONObject RemoveAt(this JSONObject jsonObject, int index) {
      if (!jsonObject.IsArray) {
        return null;
      }
      if (index >= jsonObject.Count) {
        return null;
      }
      var result = jsonObject.list[index];
      jsonObject.list.RemoveAt(index);
      return result;
    }

    public static void SeedRandom(int randomSeed) {
      random = new System.Random(randomSeed);
    }
  }
}