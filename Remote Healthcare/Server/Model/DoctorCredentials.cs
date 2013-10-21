using System;
using System.Collections.Generic;

namespace Server.Model
{
    [Serializable]
    class DoctorCredentials
    {
        public String username { get; set; }
        public String password { get; set; }
        public List<Log> logins { get; set; }

        /// <summary>
        /// Basic constructor without filling in any information.
        /// </summary>
        public DoctorCredentials()
        {
            username = "";
            password = "";
            logins = new List<Log>();
        }

        /// <summary>
        /// Constructor to set a username and password.
        /// </summary>
        /// <param name="user">The username of the Doctor.</param>
        /// <param name="pw">The password used by the Doctor.</param>
        public DoctorCredentials(String user, String pw)
        {
            username = user;
            password = pw;
            logins = new List<Log>();
        }
    }
}
