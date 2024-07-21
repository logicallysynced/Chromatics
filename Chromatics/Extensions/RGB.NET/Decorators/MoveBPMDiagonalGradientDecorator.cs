using RGB.NET.Core;
using RGB.NET.Presets.Textures.Gradients;
using System;

namespace RGB.NET.Presets.Decorators
{
    /// <inheritdoc cref="AbstractUpdateAwareDecorator" />
    /// <inheritdoc cref="IGradientDecorator" />
    /// <summary>
    /// Represents a decorator which allows to move an <see cref="T:RGB.NET.Presets.Gradients.IGradient" /> by modifying its offset based on BPM in diagonal directions.
    /// </summary>
    public class MoveBPMDiagonalGradientDecorator : AbstractUpdateAwareDecorator, IGradientDecorator
    {
        #region Properties & Fields
        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
        // ReSharper disable MemberCanBePrivate.Global

        /// <summary>
        /// Gets or sets the diagonal direction the <see cref="IGradient"/> is moved.
        /// </summary>
        public DiagonalDirection Direction { get; set; }

        /// <summary>
        /// Gets or sets the BPM (Beats Per Minute) which determines the speed of the movement.
        /// </summary>
        public int BPM { get; set; }

        private float speed;
        private const float FULL_CYCLE = 360.0f;

        // ReSharper restore MemberCanBePrivate.Global
        // ReSharper restore AutoPropertyCanBeMadeGetOnly.Global
        #endregion

        #region Constructors

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:RGB.NET.Presets.Decorators.MoveBPMDiagonalGradientDecorator" /> class.
        /// </summary>
        /// <param name="surface">The surface this decorator belongs to.</param>
        /// <param name="bpm">The BPM (Beats Per Minute) which determines the speed of the movement.</param>
        /// <param name="direction">The diagonal direction the <see cref="T:RGB.NET.Presets.Gradients.IGradient" /> is moved.</param>
        public MoveBPMDiagonalGradientDecorator(RGBSurface surface, int bpm = 120, DiagonalDirection direction = DiagonalDirection.Random)
            : base(surface)
        {
            this.BPM = bpm;
            this.Direction = direction == DiagonalDirection.Random ? GetRandomDirection() : direction;
            CalculateSpeed();
        }

        #endregion

        #region Methods

        private void CalculateSpeed()
        {
            // Convert BPM to units per second (360 units per cycle, 1 cycle per beat)
            speed = FULL_CYCLE / (60.0f / BPM);
        }

        private DiagonalDirection GetRandomDirection()
        {
            Array values = Enum.GetValues(typeof(DiagonalDirection));
            return (DiagonalDirection)values.GetValue(new Random().Next(values.Length - 1)); // Exclude Random
        }

        /// <inheritdoc />
        protected override void Update(double deltaTime)
        {
            float movement = speed * (float)deltaTime;

            switch (Direction)
            {
                case DiagonalDirection.TopLeftToBottomRight:
                    MoveDiagonal(movement);
                    break;
                case DiagonalDirection.TopRightToBottomLeft:
                    MoveDiagonal(-movement);
                    break;
                case DiagonalDirection.BottomLeftToTopRight:
                    MoveDiagonal(-movement);
                    break;
                case DiagonalDirection.BottomRightToTopLeft:
                    MoveDiagonal(movement);
                    break;
            }
        }

        private void MoveDiagonal(float movement)
        {
            foreach (IDecoratable decoratedObject in DecoratedObjects)
                if (decoratedObject is IGradient gradient)
                    gradient.Move(movement);
        }

        #endregion
    }

    /// <summary>
    /// Enum representing the diagonal directions.
    /// </summary>
    public enum DiagonalDirection
    {
        TopLeftToBottomRight,
        TopRightToBottomLeft,
        BottomLeftToTopRight,
        BottomRightToTopLeft,
        Random
    }
}
