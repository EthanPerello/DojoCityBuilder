// tile_system.cairo
use starknet::ContractAddress;

#[starknet::interface]
pub trait ITileSystem<TContractState> {
    fn buy_tile(ref self: TContractState, x: u32, y: u32);
}

#[dojo::contract]
mod tile_system {
    use starknet::{ContractAddress, get_caller_address, get_block_info};
    use dojo::world::{IWorldDispatcher};
    use dojo::model::{ModelStorage};
    use super::{ITileSystem};
    use crate::models::Tile;

    #[event]
    #[derive(Drop, starknet::Event)]
    enum Event {
        TilePurchased: TilePurchased,
    }

    #[derive(Drop, starknet::Event)]
    struct TilePurchased {
        player: ContractAddress,
        x: u32,
        y: u32,
    }

    #[storage]
    struct Storage {
        world_dispatcher: IWorldDispatcher,
    }

    #[abi(embed_v0)]
    impl TileSystemImpl of ITileSystem<ContractState> {
        fn buy_tile(ref self: ContractState, x: u32, y: u32) {
            // Get a mutable reference to the world dispatcher
            let mut world = self.world_default();

            // Get the caller's address
            let player = get_caller_address();
            
            // Generate a unique tile ID by combining block info with coordinates
            let block_info = get_block_info().unbox();
            // Convert u32 to u64 properly
            let x_u64: u64 = x.into();
            let y_u64: u64 = y.into();
            let tile_id = block_info.block_number * 10000 + x_u64 * 100 + y_u64;
            
            // Create the new tile
            let new_tile = Tile { 
                player,
                x,
                y,
                tile_id
            };
            
            // Write the tile to the world
            world.write_model(@new_tile);
            
            // Emit the event
            self.emit(TilePurchased { 
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