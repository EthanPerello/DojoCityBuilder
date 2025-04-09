// building_system.cairo
use starknet::ContractAddress;

#[starknet::interface]
pub trait IBuildingSystem<TContractState> {
    fn place_building(
        ref self: TContractState,
        x: u32,
        y: u32,
        building_type: u32,
        residents: u32,
        jobs: u32,
        shopping_space: u32,
        rotation: u32
    );
    
    // Add a function to remove a building at specific coordinates
    fn remove_building(ref self: TContractState, x: u32, y: u32);
}

#[dojo::contract]
mod building_system {
    use starknet::{ContractAddress, get_caller_address};
    use dojo::world::{IWorldDispatcher};
    use dojo::model::{ModelStorage};
    use super::{IBuildingSystem};
    use crate::models::Building;

    #[event]
    #[derive(Drop, starknet::Event)]
    enum Event {
        BuildingPlaced: BuildingPlaced,
        BuildingRemoved: BuildingRemoved,
    }

    #[derive(Drop, starknet::Event)]
    struct BuildingPlaced {
        player: ContractAddress,
        x: u32,
        y: u32,
        building_type: u32,
    }
    
    #[derive(Drop, starknet::Event)]
    struct BuildingRemoved {
        player: ContractAddress,
        x: u32,
        y: u32,
    }

    #[storage]
    struct Storage {
        world_dispatcher: IWorldDispatcher,
    }

    #[abi(embed_v0)]
    impl BuildingSystemImpl of IBuildingSystem<ContractState> {
        fn place_building(
            ref self: ContractState,
            x: u32,
            y: u32,
            building_type: u32,
            residents: u32,
            jobs: u32,
            shopping_space: u32,
            rotation: u32
        ) {
            // Get a mutable reference to the world dispatcher
            let mut world = self.world_default();

            // Get the caller's address
            let player = get_caller_address();

            // Create the new building
            let new_building = Building {
                player,
                x,
                y,
                building_type,
                residents,
                jobs,
                shopping_space,
                happy_residents: 0,
                rotation
            };
            
            // Write the building to the world
            world.write_model(@new_building);
            
            // Emit the event
            self.emit(BuildingPlaced { 
                player,
                x,
                y,
                building_type
            });
        }
        
        fn remove_building(ref self: ContractState, x: u32, y: u32) {
            // Get a mutable reference to the world dispatcher
            let mut world = self.world_default();

            // Get the caller's address
            let player = get_caller_address();
            
            // Create a reference to the building we want to erase
            let building = Building {
                player,
                x,
                y,
                // Non-key fields don't matter for deletion
                building_type: 0,
                residents: 0,
                jobs: 0,
                shopping_space: 0,
                happy_residents: 0,
                rotation: 0
            };
            
            // Erase the building from the world
            world.erase_model(@building);
            
            // Emit the event
            self.emit(BuildingRemoved {
                player,
                x,
                y
            });
        }
    }

    #[generate_trait]
    impl InternalImpl of InternalTrait {
        fn world_default(self: @ContractState) -> dojo::world::WorldStorage {
            self.world(@"city_builder")
        }
    }
}