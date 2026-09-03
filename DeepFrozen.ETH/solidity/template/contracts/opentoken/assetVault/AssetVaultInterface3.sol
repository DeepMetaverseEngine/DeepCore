// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import "../../nameserver/preclude/Preclude.sol";

import "../../nameserver/nameServiceRef/NameServiceRefInterface.sol";

import "./AssetVaultEvent.sol";
import "./AssetVaultType.sol";

interface AssetVaultInterface3 is NameServiceRefInterface, AssetVaultEvent {

    function depositErc721(
        bytes32 erc721TokenName,
        address owner,
        uint256 tokenId
    ) external;

    function depositErc721s(
        bytes32[] memory erc721TokenNames,
        address owner,
        uint256[] memory tokenIds
    ) external;

    function withdrawErc721(
        address owner,
        AssetVaultType.Erc721Param memory param,
        uint256 traceId
    ) external;

    function withdrawErc721s(
        address owner,
        AssetVaultType.Erc721Param[] memory params,
        uint256 traceId
    ) external;

    function meltErc721(
        AssetVaultType.Erc721Param memory param
    ) external;

    function meltErc721s(
        AssetVaultType.Erc721Param[] memory params
    ) external;
}
