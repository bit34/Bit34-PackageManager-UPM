using Com.Bit34games.PackageManager.Constants;


namespace Com.Bit34games.PackageManager.VOs
{
    internal class PackageManagerErrorForGitCommandFailedVO : PackageManagerErrorVO
    {
        //  MEMBERS
        /// <summary>What the tool was trying to do, in the user's terms.</summary>
        public readonly string operation;
        /// <summary>The failed command and git's own stderr.</summary>
        public readonly string details;


        //  CONSTRUCTOR
        public PackageManagerErrorForGitCommandFailedVO(string operation, string details)
         : base(PackageManagerErrors.GitCommandFailed)
        {
            this.operation = operation;
            this.details   = details;
        }
    }
}
