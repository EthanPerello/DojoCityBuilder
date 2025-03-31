// reset_system.cairo
use starknet::ContractAddress;

#[starknet::interface]
pub trait IResetSystem<TContractState> {
    fn reset_player_data(ref self: TContractState);
}

#[dojo::contract]
mod reset_system {
    use starknet::{ContractAddress, get_caller_address};
    use dojo::world::{IWorldDispatcher};
    use dojo::model::{ModelStorage, ModelStorageTest};
    use super::{IResetSystem};
    use crate::models::{Building, Tile, Player};

    #[event]
    #[derive(Drop, starknet::Event)]
    enum Event {
        PlayerDataReset: PlayerDataReset,
    }

    #[derive(Drop, starknet::Event)]
    struct PlayerDataReset {
        player: ContractAddress,
    }

    #[storage]
    struct Storage {
        world_dispatcher: IWorldDispatcher,
    }

    #[abi(embed_v0)]
    impl ResetSystemImpl of IResetSystem<ContractState> {
        fn reset_player_data(ref self: ContractState) {
            // Get the world instance
            let mut world = self.world_default();

            // Get the player address
            let player = get_caller_address();
            
            // Reset player data to default values
            let initial_money = 1000_u128;
            let player_data = Player {
                player,
                money: initial_money
            };
            
            // Write the reset player data to the world
            world.write_model(@player_data);
            
            // Note: In a production environment, we would want to:
            // 1. Query for all tiles owned by the player
            // 2. Query for all buildings owned by the player
            // 3. Delete them
            // However, in the current version of Dojo, we don't have a query mechanism
            // to find all entities owned by a player. This would require a different
            // data modeling approach or external indexing.
            
            // Emit an event to notify about the reset
            self.emit(PlayerDataReset { player });
        }
    }

    #[generate_trait]
    impl InternalImpl of InternalTrait {
        fn world_default(self: @ContractState) -> dojo::world::WorldStorage {
            self.world(@"city_builder")
        }
    }
}