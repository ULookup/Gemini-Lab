#nullable enable
using GeminiLab.Modules.Pet;
using NUnit.Framework;
using UnityEngine;

namespace GeminiLab.Tests.EditMode
{
    public sealed class PetPlayerInputControllerTests
    {
        [Test]
        public void ComposeMovementVector_DiagonalInput_IsNormalized()
        {
            Vector2 movement = PetPlayerInputController.ComposeMovementVector(
                left: false,
                right: true,
                up: true,
                down: false);

            Assert.AreEqual(1f, movement.magnitude, 0.0001f);
            Assert.Greater(movement.x, 0f);
            Assert.Greater(movement.y, 0f);
        }

        [Test]
        public void ComposeMovementVector_OppositeInputs_CancelOut()
        {
            Vector2 movement = PetPlayerInputController.ComposeMovementVector(
                left: true,
                right: true,
                up: false,
                down: false);

            Assert.AreEqual(Vector2.zero, movement);
        }

        [Test]
        public void ComposeRawInputVector_DiagonalInput_PreservesBothAxes()
        {
            Vector2 rawInput = PetPlayerInputController.ComposeRawInputVector(
                left: false,
                right: true,
                up: true,
                down: false);

            Assert.AreEqual(new Vector2(1f, 1f), rawInput);
        }
    }
}
