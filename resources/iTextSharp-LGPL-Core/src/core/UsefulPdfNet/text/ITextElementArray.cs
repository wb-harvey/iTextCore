using System;

namespace UsefulPdfNet.text {
    /// <summary>
    /// Interface for a text element to which other objects can be added.
    /// </summary>
    /// <seealso cref="T:UsefulPdfNet.text.Phrase"/>
    /// <seealso cref="T:UsefulPdfNet.text.Paragraph"/>
    /// <seealso cref="T:UsefulPdfNet.text.Section"/>
    /// <seealso cref="T:UsefulPdfNet.text.ListItem"/>
    /// <seealso cref="T:UsefulPdfNet.text.Chapter"/>
    /// <seealso cref="T:UsefulPdfNet.text.Anchor"/>
    /// <seealso cref="T:UsefulPdfNet.text.Cell"/>
    public interface ITextElementArray : IElement {
        /// <summary>
        /// Adds an object to the TextElementArray.
        /// </summary>
        /// <param name="o">an object that has to be added</param>
        /// <returns>true if the addition succeeded; false otherwise</returns>
        bool Add(Object o);
    }
}
