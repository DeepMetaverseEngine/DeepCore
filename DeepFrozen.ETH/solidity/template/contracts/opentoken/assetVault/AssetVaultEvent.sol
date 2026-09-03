// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "./AssetVaultType.sol";

interface AssetVaultEvent {

    event DepositErc20(address owner, bytes32[] tokenNames, uint256[] amounts, address from);
    event WithdrawErc20(address owners, bytes32[] tokenNames, uint256[] amounts, uint256 traceId);

    //=====================

    event DepositErc1155(address owner, bytes32[] tokenNames, uint256[] tokenIds, uint256[] amounts, address from);
    event WithdrawErc1155(address owner, bytes32[] tokenNames, uint256[] tokenIds, uint256[] amounts, uint256 traceId);

    //=====================

    event DepositErc721(address owner, bytes32[] tokenNames, uint256[] tokenIds, address from);
    event WithdrawErc721(address owner, AssetVaultType.Erc721Param[] tokens, uint256 traceId);
    event MeltErc721(bytes32 tokenName, uint256 tokenId, bytes32[] attributeNames, uint256[] uint256Values, bytes32[] bytes32Values, address[] addressValues, bytes[] bytesValues);

}
