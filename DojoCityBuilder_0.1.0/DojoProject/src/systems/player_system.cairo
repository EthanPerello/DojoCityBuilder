// player_system.cairo
use starknet::ContractAddress;

#[starknet::interface]
pub trait IPlayerSystem<TContractState> {
    fn initialize_player(ref self: TContractState);
    fn update_money(ref self: TContractState, new_money: u128);
}

#[dojo::contract]
mod player_system {
    use starknet::{ContractAddress, get_caller_address};
    use dojo::world::{IWorldDispatcher};
    use dojo::model::ModelStorage;
    use super::{IPlayerSystem};
    use crate::models::Player;

    #[event]
    #[derive(Drop, starknet::Event)]
    enum Event {
        PlayerInitialized: PlayerInitialized,
        MoneyUpdated: MoneyUpdated,
    }

    #[derive(Drop, starknet::Event)]
    struct PlayerInitialized {
        player: ContractAddress,
        initial_money: u128,
    }

    #[derive(Drop, starknet::Event)]
    struct MoneyUpdated {
        player: ContractAddress,
        new_money: u128,
    }

    #[storage]
    struct Storage {
        world_dispatcher: IWorldDispatcher,
    }

    #[abi(embed_v0)]
    impl PlayerSystemImpl of IPlayerSystem<ContractState> {
        fn initialize_player(ref self: ContractState) {
            // Get a mutable reference to the world dispatcher
            let mut world = self.world_default();

            // Get the caller's address
            let player = get_caller_address();
            let initial_money = 1000_u128;
            
            // Create the new player
            let new_player = Player {
                player,
                money: initial_money,
            };
            
            // Write the player to the world
            world.write_model(@new_player);
            
            // Emit the event
            self.emit(PlayerInitialized { 
                player,
                initial_money
            });
        }

        fn update_money(ref self: ContractState, new_money: u128) {
            // Get a mutable reference to the world dispatcher
            let mut world = self.world_default();

            // Get the caller's address
            let player = get_caller_address();

            // Read current player data
            let mut current_player: Player = world.read_model(player);
            current_player.money = new_money;
            
            // Update the player data
            world.write_model(@current_player);
            
            // Emit the event
            self.emit(MoneyUpdated { 
                player,
                new_money 
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