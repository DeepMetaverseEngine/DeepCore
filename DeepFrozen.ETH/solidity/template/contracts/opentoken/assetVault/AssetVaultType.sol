// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

library AssetVaultType {

    uint256 constant ERC20_DEPOSIT_STRATEGY_UNKNOWN = 0;
    uint256 constant ERC20_DEPOSIT_STRATEGY_BURN = 1;
    uint256 constant ERC20_DEPOSIT_STRATEGY_TRANSFER = 2;
    uint256 constant ERC20_DEPOSIT_STRATEGY_STORE = 3;

    uint256 constant ERC20_WITHDRAW_STRATEGY_UNKNOWN = 0;
    uint256 constant ERC20_WITHDRAW_STRATEGY_MINT = 1;
    uint256 constant ERC20_WITHDRAW_STRATEGY_TRANSFER = 2;
    uint256 constant ERC20_WITHDRAW_STRATEGY_RESTORE_MINT = 3;
    uint256 constant ERC20_WITHDRAW_STRATEGY_RESTORE_REVERT = 4;
    uint256 constant ERC20_WITHDRAW_STRATEGY_RESTORE_TRANSFER = 5;


    uint256 constant ERC1155_DEPOSIT_STRATEGY_UNKNOWN = 0;
    uint256 constant ERC1155_DEPOSIT_STRATEGY_BURN = 1;
    uint256 constant ERC1155_DEPOSIT_STRATEGY_TRANSFER = 2;
    uint256 constant ERC1155_DEPOSIT_STRATEGY_STORE = 3;

    uint256 constant ERC1155_WITHDRAW_STRATEGY_UNKNOWN = 0;
    uint256 constant ERC1155_WITHDRAW_STRATEGY_MINT = 1;
    uint256 constant ERC1155_WITHDRAW_STRATEGY_TRANSFER = 2;
    uint256 constant ERC1155_WITHDRAW_STRATEGY_RESTORE_MINT = 3;
    uint256 constant ERC1155_WITHDRAW_STRATEGY_RESTORE_REVERT = 4;
    uint256 constant ERC1155_WITHDRAW_STRATEGY_RESTORE_TRANSFER = 5;

    struct Erc721Param {
        bytes32 erc721TokenName;
        uint256 tokenId;
        bytes32[] attributeNames;
        uint256[] uint256Values;
        bytes32[] bytes32Values;
        address[] addressValues;
        bytes[] bytesValues;
        Erc721AffiliateParam[] affiliateParams;
    }

    struct Erc721AffiliateParam {
        bytes32 erc721TokenName;
        uint256 tokenId;
        bytes32[] attributeNames;
        uint256[] uint256Values;
        bytes32[] bytes32Values;
        address[] addressValues;
        bytes[] bytesValues;
    }
}
