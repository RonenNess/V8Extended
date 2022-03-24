using Microsoft.ClearScript.V8;


namespace V8Extended
{
    /// <summary>
    /// V8 extension module base class.
    /// </summary>
    public abstract class V8Extender
    {
        /// <summary>
        /// The V8 engine instance we initialized on.
        /// Only available after 'Extend' was called.
        /// </summary>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected V8ScriptEngine Engine { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        /// <summary>
        /// Extend the V8 engine by attaching itself to it.
        /// </summary>
        /// <param name="engine">Engine instance to extend.</param>
        /// <returns>True if succeed, false if couldn't apply extension for whatever reason.</returns>
        public bool Extend(V8ScriptEngine engine)
        {
            Engine = engine;

            // setup exceptions handling base
            try
            {
                Engine.Execute(@"
const __hostExceptionsHandler__ = { 
    checkAndThrow: function() { 
        if (this.error) { 
            let msg = this.error;
            this.error = null;
            throw new Error(msg);
        } 
    } 
}");
            }
            catch (Exception ex) { }

            return ExtendImpl();
        }

        /// <summary>
        /// Implement the extension logic.
        /// Must be implemented by all derived classes.
        /// </summary>
        /// <returns>True if succeed, false if couldn't apply extension for whatever reason.</returns>
        protected abstract bool ExtendImpl();

        /// <summary>
        /// Pass an exception to JavaScript side.
        /// </summary>
        public void PassExceptionToJs(Exception ex)
        {
            Engine.Execute($"__hostExceptionsHandler__.error = `{ex.Message.Replace("`", "'")}`;");
        }
    }
}
