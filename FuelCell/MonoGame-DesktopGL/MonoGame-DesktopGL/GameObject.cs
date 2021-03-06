// Copyright (C) Microsoft Corporation. All rights reserved.

using Fizzyo_Library;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace FizzyoMonoGame
{
    class GameObject
    {
        public Model Model { get; set; }
        public Vector3 Position { get; set; }
        public BoundingSphere BoundingSphere { get; set; }

        public GameObject()
        {
            Model = null;
            Position = Vector3.Zero;
            BoundingSphere = new BoundingSphere();
        }

        protected BoundingSphere CalculateBoundingSphere()
        {
            BoundingSphere mergedSphere = new BoundingSphere();
            BoundingSphere[] boundingSpheres;
            int index = 0;
            int meshCount = Model.Meshes.Count;

            boundingSpheres = new BoundingSphere[meshCount];
            foreach (ModelMesh mesh in Model.Meshes)
            {
                boundingSpheres[index++] = mesh.BoundingSphere;
            }

            mergedSphere = boundingSpheres[0];
            if ((Model.Meshes.Count) > 1)
            {
                index = 1;
                do
                {
                    mergedSphere = BoundingSphere.CreateMerged(mergedSphere, 
                        boundingSpheres[index]);
                    index++;
                } while (index < Model.Meshes.Count);
            }
            mergedSphere.Center.Y = 0;
            return mergedSphere;
        }

        internal void DrawBoundingSphere(Matrix view, Matrix projection, 
            GameObject boundingSphereModel)
        {
            Matrix scaleMatrix = Matrix.CreateScale(BoundingSphere.Radius);
            Matrix translateMatrix = 
                Matrix.CreateTranslation(BoundingSphere.Center);
            Matrix worldMatrix = scaleMatrix * translateMatrix;

            foreach (ModelMesh mesh in boundingSphereModel.Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = worldMatrix;
                    effect.View = view;
                    effect.Projection = projection;
                }
                mesh.Draw();
            }
        }
    }

    class FuelCell : GameObject
    {
        public bool Retrieved { get; set; }

        public FuelCell()
            : base()
        {
            Retrieved = false;
        }

        public void LoadContent(ContentManager content, string modelName)
        {
            Model = content.Load<Model>(modelName);
            Position = Vector3.Down;

            BoundingSphere = CalculateBoundingSphere();

            BoundingSphere scaledSphere;
            scaledSphere = BoundingSphere;
            scaledSphere.Radius *= GameConstants.FuelCellBoundingSphereFactor;
            BoundingSphere = 
                new BoundingSphere(scaledSphere.Center, scaledSphere.Radius);
        }

        public void Draw(Matrix view, Matrix projection)
        {
            Matrix[] transforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(transforms);
            Matrix translateMatrix = Matrix.CreateTranslation(Position);
            Matrix worldMatrix = translateMatrix;

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = 
                        worldMatrix * transforms[mesh.ParentBone.Index];
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                }
                mesh.Draw();
            }
        }

        internal void Update(BoundingSphere vehicleBoundingSphere)
        {
            if (vehicleBoundingSphere.Intersects(this.BoundingSphere))
                this.Retrieved = true;
        }
    }

    class Barrier : GameObject
    {
        public string BarrierType { get; set; }

        public Barrier()
            : base()
        {
            BarrierType = null;
        }

        public void LoadContent(ContentManager content, string modelName)
        {
            Model = content.Load<Model>(modelName);
            BarrierType = modelName;
            Position = Vector3.Down;
            BoundingSphere = CalculateBoundingSphere();

            BoundingSphere scaledSphere;
            scaledSphere = BoundingSphere;
            scaledSphere.Radius *= GameConstants.BarrierBoundingSphereFactor;
            BoundingSphere = 
                new BoundingSphere(scaledSphere.Center, scaledSphere.Radius);
        }

        public void Draw(Matrix view, Matrix projection)
        {
            Matrix[] transforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(transforms);
            Matrix translateMatrix = Matrix.CreateTranslation(Position);
            Matrix worldMatrix = translateMatrix;

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = 
                        worldMatrix * transforms[mesh.ParentBone.Index];
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                }
                mesh.Draw();
            }
        }
    }

    class FuelCarrier : GameObject
    {
        public float ForwardDirection { get; set; }
        public int MaxRange { get; set; }

        InputState Input;

        Game1 gameInstance;

        public FuelCarrier(Game1 game)
            : base()
        {
            gameInstance = game;

            ForwardDirection = 0.0f;
            MaxRange = GameConstants.MaxRange;

            // retrieve the Input manager
            Input = (InputState)game.Services.GetService(
                typeof(InputState));
        }

        public void LoadContent(ContentManager content, string modelName)
        {
            Model = content.Load<Model>(modelName);
            BoundingSphere = CalculateBoundingSphere();

            BoundingSphere scaledSphere;
            scaledSphere = BoundingSphere;
            scaledSphere.Radius *= 
                GameConstants.FuelCarrierBoundingSphereFactor;
            BoundingSphere = 
                new BoundingSphere(scaledSphere.Center, scaledSphere.Radius);
        }

        internal void Reset()
        {
            Position = Vector3.Zero;
            ForwardDirection = 0f;
        }

        public void Draw(Matrix view, Matrix projection)
        {
            Matrix[] transforms = new Matrix[Model.Bones.Count];
            Model.CopyAbsoluteBoneTransformsTo(transforms);
            Matrix worldMatrix = Matrix.Identity;
            Matrix rotationYMatrix = Matrix.CreateRotationY(ForwardDirection);
            Matrix translateMatrix = Matrix.CreateTranslation(Position);

            worldMatrix = rotationYMatrix * translateMatrix;

            foreach (ModelMesh mesh in Model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = 
                        worldMatrix * transforms[mesh.ParentBone.Index];
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                }
                mesh.Draw();
            }
        }

        public void Update(Barrier[] barriers)
        {
            Vector3 futurePosition = Position;
            float turnAmount = 0;
            Vector2 LeftThumbstick = Vector2.Zero; // Input.GetThumbStickLeft(PlayerIndex.One);

            if (Input.IsKeyPressed(Keys.A))
            {
                turnAmount = 1;
            }
            else if (Input.IsKeyPressed(Keys.D))
            {
                turnAmount = -1;
            }
            else if (LeftThumbstick.X != 0)
            {
                turnAmount = -LeftThumbstick.X;
            }
            ForwardDirection += turnAmount * GameConstants.TurnSpeed;
            Matrix orientationMatrix = Matrix.CreateRotationY(ForwardDirection);

            Vector3 movement = Vector3.Zero;
            if (Input.IsKeyPressed(Keys.W))
            {
                movement.Z = 1;
            }
            else if (Input.IsKeyPressed(Keys.S))
            {
                movement.Z = -1;
            }
            else if (LeftThumbstick.Y != 0)
            {
                movement.Z = LeftThumbstick.Y;
            }

            //If player is moving, extract power
            if (gameInstance.CanMove && (movement.Z > 0 || movement.Z < 0))
            {
                gameInstance.CurrentPower -= GameConstants.MoveCost;
            }

            if (gameInstance.CanMove && gameInstance.CurrentPower > 1)
            {
                Vector3 speed = Vector3.Transform(movement, orientationMatrix);
                speed *= GameConstants.Velocity;
                futurePosition = Position + speed;

                if (ValidateMovement(futurePosition, barriers))
                {
                    Position = futurePosition;

                    BoundingSphere updatedSphere;
                    updatedSphere = BoundingSphere;

                    updatedSphere.Center.X = Position.X;
                    updatedSphere.Center.Z = Position.Z;
                    BoundingSphere = new BoundingSphere(updatedSphere.Center,
                        updatedSphere.Radius);
                }
            }
        }

        private bool ValidateMovement(Vector3 futurePosition, 
            Barrier[] barriers)
        {
            BoundingSphere futureBoundingSphere = BoundingSphere;
            futureBoundingSphere.Center.X = futurePosition.X;
            futureBoundingSphere.Center.Z = futurePosition.Z;
            
            //Don't allow off-terrain driving
            if ((Math.Abs(futurePosition.X) > MaxRange) || 
                (Math.Abs(futurePosition.Z) > MaxRange))
                return false;
            //Don't allow driving through a barrier
            if (CheckForBarrierCollision(futureBoundingSphere, barriers))
                return false;

            return true;
        }

        private bool CheckForBarrierCollision(
            BoundingSphere vehicleBoundingSphere, Barrier[] barriers)
        {
            for (int curBarrier = 0; curBarrier < barriers.Length; curBarrier++)
            {
                if (vehicleBoundingSphere.Intersects(
                    barriers[curBarrier].BoundingSphere))
                    return true;
            }
            return false;
        }
    }

    class Camera
    {
        public Vector3 AvatarHeadOffset { get; set; }
        public Vector3 TargetOffset { get; set; }
        public Matrix ViewMatrix { get; set; }
        public Matrix ProjectionMatrix { get; set; }

        public Camera()
        {
            AvatarHeadOffset = new Vector3(0, 7, -15);
            TargetOffset = new Vector3(0, 5, 0);
            ViewMatrix = Matrix.Identity;
            ProjectionMatrix = Matrix.Identity;
        }

        public void Update(float avatarYaw, Vector3 position, float aspectRatio)
        {
            Matrix rotationMatrix = Matrix.CreateRotationY(avatarYaw);

            Vector3 transformedheadOffset = 
                Vector3.Transform(AvatarHeadOffset, rotationMatrix);
            Vector3 transformedReference = 
                Vector3.Transform(TargetOffset, rotationMatrix);

            Vector3 cameraPosition = position + transformedheadOffset;
            Vector3 cameraTarget = position + transformedReference;

            //Calculate the camera's view and projection
            // matrices based on current values.
            ViewMatrix = 
                Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);
            ProjectionMatrix = 
                Matrix.CreatePerspectiveFieldOfView(
                    MathHelper.ToRadians(GameConstants.ViewAngle), aspectRatio, 
                    GameConstants.NearClip, GameConstants.FarClip);
        }
    }
}
