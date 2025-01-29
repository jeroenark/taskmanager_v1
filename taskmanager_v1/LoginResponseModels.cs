using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace taskmanager_v1
{
    // Login Response and Data Classes
    public class LoginResponse
    {
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string[] Message { get; set; }

        [JsonProperty("data")]
        public LoginData Data { get; set; }

    }

    public class LoginData
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("access_token_expiry")]
        public int AccessTokenExpiry { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("refresh_token_expiry")]
        public int RefreshTokenExpiry { get; set; }
    }

    // Register Response Class
    public class RegisterResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string[] Message { get; set; }
    }

    // Task Item Class
    public class TaskItem
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        private string _deadline;
        [JsonProperty("deadline")]
        public string Deadline
        {
            get => _deadline;
            set
            {
                _deadline = value;
                // Convert string deadline to DateTime for sorting
                DeadlineDate = DateTime.TryParse(value, out DateTime result) ? result : (DateTime?)null;
            }
        }

        public DateTime? DeadlineDate { get; private set; } // For sorting and filtering by DateTime

        [JsonProperty("completed")]
        public string Completed { get; set; }
    }

    // API Response Class
    public class ApiResponse
    {
        [JsonProperty("statusCode")]
        public int StatusCode { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string[] Message { get; set; }

        [JsonProperty("data")]
        public TaskData Data { get; set; }
    }

    // Task Data Class
    public class TaskData
    {
        [JsonProperty("rows_returned")]
        public int RowsReturned { get; set; }

        [JsonProperty("tasks")]
        public List<TaskItem> Tasks { get; set; }
    }
}
