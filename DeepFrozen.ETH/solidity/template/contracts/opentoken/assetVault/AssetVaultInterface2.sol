// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/nameServiceRef/NameServiceRefInterface.sol";

import "./AssetVaultEvent.sol";

interface AssetVaultInterface2 is NameServiceRefInterface, AssetVaultEvent {

    function depositErc1155(
        bytes32 erc1155TokenName,
        address owner,
        uint256 tokenId,
        uint256 amount
    ) external;

    function depositErc1155s(
        bytes32[] memory erc1155TokenNames,
        address owner,
        uint256[] memory tokenIds,
        uint256[] memory amounts
    ) external;

    function withdrawErc1155(
        bytes32 erc1155TokenName,
        address owner,
        uint256 tokenId,
        uint256 amount,
        uint256 traceId
    ) external;

    function withdrawErc1155s(
        bytes32[] memory erc1155TokenNames,
        address owner,
        uint256[] memory tokenIds,
        uint256[] memory amounts,
        uint256 traceId
    ) external;
}
