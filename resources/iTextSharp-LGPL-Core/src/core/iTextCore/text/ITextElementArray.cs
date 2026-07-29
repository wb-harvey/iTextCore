using System;

namespace iTextCore.text {
    /// <summary>
    /// Interface for a text element to which other objects can be added.
    /// </summary>
    /// <seealso cref="T:iTextCore.text.Phrase"/>
    /// <seealso cref="T:iTextCore.text.Paragraph"/>
    /// <seealso cref="T:iTextCore.text.Section"/>
    /// <seealso cref="T:iTextCore.text.ListItem"/>
    /// <seealso cref="T:iTextCore.text.Chapter"/>
    /// <seealso cref="T:iTextCore.text.Anchor"/>
    /// <seealso cref="T:iTextCore.text.Cell"/>
    public interface ITextElementArray : IElement {
        /// <summary>
        /// Adds an object to the TextElementArray.
        /// </summary>
        /// <param name="o">an object that has to be added</param>
        /// <returns>true if the addition succeeded; false otherwise</returns>
        bool Add(Object o);
    }
}
